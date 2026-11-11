import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { chromium } from 'playwright';
import { startStore, root } from './run.mjs';

const store = await startStore();
let browser;
try {
  browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage({ viewport: { width: 1365, height: 1000 } });
  const errors = [];
  const assets = [];
  const invocations = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    const url = new URL(response.url());
    if (['script', 'stylesheet'].includes(response.request().resourceType())) assets.push({ url, status: response.status() });
    if (url.pathname.endsWith('/content/invocations')) invocations.push({ url, status: response.status() });
  });
  await page.goto('http://localhost:7071/');
  await page.getByRole('heading', { name: 'Little things. Good company.' }).waitFor();
  assert.equal(await page.locator('.product-card').count(), 4);
  assert.ok(assets.every(asset => asset.url.port === '7071' && asset.status === 200), 'Hosted shell supplies all scripts and styles');
  assert.ok(invocations.some(request => request.url.port === '7072' && request.status === 200));
  fs.mkdirSync(path.join(root, 'artifacts'), { recursive: true });
  await page.screenshot({ path: path.join(root, 'artifacts/store-desktop.png'), fullPage: true });
  await page.getByRole('button', { name: 'Add to bag' }).first().click();
  await page.waitForURL('http://localhost:7071/cart');
  await page.locator('[data-product="mug"]').waitFor();
  assert.equal(await page.locator('[data-quantity]').innerText(), '1');
  assert.equal(new URL(page.url()).search, '', 'Cart URL has no identifiers');
  assert.equal(await page.evaluate(() => sessionStorage.getItem('teststore.pending-cart')), null, 'Successful add clears handoff');
  assert.ok(invocations.some(request => request.url.port === '7073' && request.status === 200));
  await page.reload();
  await page.locator('[data-quantity]').waitFor();
  assert.equal(await page.locator('[data-quantity]').innerText(), '1', 'Reload does not duplicate an add');
  await page.getByRole('button', { name: '+', exact: true }).click();
  await page.waitForFunction(() => document.querySelector('[data-quantity]')?.textContent === '2');
  const cartId = (await context.cookies('http://localhost:7071')).find(cookie => cookie.name === 'TestStore.Cart').value;
  const noCsrf = await context.request.post('http://localhost:7073/__heimdall/v1/content/invocations', {
    headers: { 'X-Heimdall-Content-Invocation': 'cart.quantity' },
    data: { cartId, productId: 'mug', quantity: 9, operationId: '11111111111111111111111111111111' }
  });
  assert.equal(noCsrf.status(), 400);
  assert.equal(noCsrf.headers()['x-heimdall-antiforgery'], 'invalid');
  // Each preview must work with requests to every other app blocked.
  for (const port of [7072,7073]) {
    const previewContext = await browser.newContext();
    const preview = await previewContext.newPage();
    await preview.route('**/*', route => new URL(route.request().url()).port === String(port) ? route.continue() : route.abort());
    await preview.goto('http://localhost:'+port+'/');
    assert.equal(await preview.locator('header,nav,footer').count(),0);
    if (port === 7072) {
      await preview.getByRole('button',{name:'Add to bag'}).first().click();
      await preview.getByRole('status').filter({hasText:'Preview selected: Everyday mug'}).waitFor();
      assert.equal(preview.url(),'http://localhost:7072/');
    } else {
      await preview.locator('[data-product="mug"]').waitFor();
      await preview.getByRole('button',{name:'+',exact:true}).click();
      await preview.waitForFunction(() => document.querySelector('[data-quantity]')?.textContent === '2');
      await preview.reload();
      await preview.waitForFunction(() => document.querySelector('[data-quantity]')?.textContent === '2');
      await preview.getByRole('button',{name:'Remove',exact:true}).click();
      await preview.getByText('Your bag is waiting for something good.').waitFor();
    }
    await preview.screenshot({path:path.join(root,'artifacts/preview-'+port+'.png'),fullPage:true});
    await previewContext.close();
  }
  await page.getByRole('button', { name: 'Remove', exact: true }).click();
  await page.getByText('Your bag is waiting for something good.').waitFor();
  await page.goto('http://localhost:7071/');
  await page.locator('.product-card').first().waitFor();
  let acknowledgeLostResponse;
  const lostResponse = new Promise(resolve => { acknowledgeLostResponse = resolve; });
  let dropCartResponse = true;
  await page.route('http://localhost:7073/__heimdall/v1/content/invocations**', async route => {
    if (dropCartResponse && route.request().method() === 'POST') {
      const response = await route.fetch();
      if (response.ok()) {
        dropCartResponse = false;
        await route.abort('failed');
        acknowledgeLostResponse();
        return;
      }
      await route.fulfill({ response });
      return;
    }
    await route.continue();
  });
  await page.getByRole('button', { name: 'Add to bag' }).nth(1).click();
  await page.waitForURL('http://localhost:7071/cart');
  await Promise.race([lostResponse, new Promise((_,reject) => { const timer=setTimeout(() => reject(new Error('Expected dropped Cart response')),15000); timer.unref(); })]);
  assert.ok(await page.evaluate(() => sessionStorage.getItem('teststore.pending-cart')), 'Lost response retains pending operation');
  await page.reload();
  await page.locator('[data-product="notebook"]').waitFor();
  assert.equal(await page.locator('[data-quantity]').innerText(), '1', 'Retry after persisted-but-lost response adds only once');
  assert.equal(await page.evaluate(() => sessionStorage.getItem('teststore.pending-cart')), null);
  await page.getByRole('button', { name: 'Place demo order' }).click();
  await page.getByText('A pretend order. A real round trip.').waitFor();
  await page.reload();
  await page.getByText('A pretend order. A real round trip.').waitFor();
  assert.equal(await page.locator('.cart-row').count(), 0, 'Reload does not replay an old add after checkout');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('http://localhost:7071/');
  await page.locator('.product-card').first().waitFor();
  assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Mobile has no horizontal overflow');
  await page.screenshot({ path: path.join(root, 'artifacts/store-mobile.png'), fullPage: true });
  // Block storage: selection must stay on Catalog instead of silently losing the add.
  const blocked = await browser.newContext();
  await blocked.addInitScript(() => { Storage.prototype.setItem = () => { throw new DOMException('Blocked', 'SecurityError'); }; });
  const blockedPage = await blocked.newPage();
  await blockedPage.goto('http://localhost:7071/');
  await blockedPage.getByRole('button', { name: 'Add to bag' }).first().click();
  await blockedPage.getByRole('alert').filter({ hasText: "couldn't save the selection" }).waitFor();
  assert.equal(blockedPage.url(), 'http://localhost:7071/');
  await blockedPage.goto('http://localhost:7071/cart');
  await blockedPage.getByText('Your bag is waiting for something good.').waitFor();
  await blocked.close();
  assert.deepEqual(errors, []);
  console.log('PASS: AppHost pages and independent backend previews, shell-owned assets, cross-origin invocations, clean URLs, storage handoff/cleanup, blocked-storage fallback, CSRF, quantity/remove, idempotent reload, checkout, and mobile layout.');
} catch (error) {
  console.error(store.output());
  throw error;
} finally {
  await browser?.close();
  store.stop();
}
