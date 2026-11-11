import { spawn, spawnSync } from 'node:child_process';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';

export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const apps = [['AppHost', 7071], ['Catalog', 7072], ['Cart', 7073]];
export async function startStore() {
  const built = spawnSync('dotnet', ['build', path.join(root, 'TestStore.slnx'), '-m:1', '-v:minimal'], { stdio: 'inherit', windowsHide: true });
  if (built.status !== 0) throw new Error('Store build failed.');
  const func = process.env.FUNC_PATH || 'func';
  const children = [];
  let output = '';
  let launchError;
  function stop() {
    for (const child of children) {
      if (!child.pid || child.exitCode !== null) continue;
      if (process.platform === 'win32') spawnSync('taskkill', ['/pid', String(child.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
      else child.kill('SIGTERM');
    }
  }
  try {
    for (const [name, port] of apps) {
      const project = path.join(root, `TestStore.${name}`);
      const child = spawn(func, ['start', '--no-build', '--script-root', path.join(project, 'bin/Debug/net10.0'), '--port', String(port)], {
        cwd: project, windowsHide: true, env: { ...process.env, AZURE_FUNCTIONS_CORE_TOOLS_TELEMETRY_OPTOUT: '1' }
      });
      children.push(child);
      child.on('error', error => { launchError = error; });
      const collect = chunk => {
        const line = `[${name}] ${chunk}`;
        output = (output + line).slice(-50000);
        if (process.env.STORE_VERBOSE === '1') process.stdout.write(line);
      };
      child.stdout.on('data', collect);
      child.stderr.on('data', collect);
    }
    for (let attempt = 0; attempt < 120; attempt++) {
      if (launchError) throw new Error('Install Azure Functions Core Tools v4 or set FUNC_PATH to its executable. ' + launchError.message);
      if (children.some(child => child.exitCode !== null)) throw new Error('A Function host exited. ' + output);
      const ready = await Promise.all(apps.map(async ([, port]) => {
        try { return (await fetch(`http://localhost:${port}/${port === 7071 ? "" : "__heimdall/v1/csrf"}`, { signal: AbortSignal.timeout(1000) })).ok; }
        catch { return false; }
      }));
      if (ready.every(Boolean)) return { stop, output: () => output };
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    throw new Error('Timed out starting the Function Apps. ' + output);
  } catch (error) { stop(); throw error; }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const running = await startStore();
  console.log('Store: http://localhost:7071 · Catalog backend: :7072 · Cart backend: :7073');
  console.log('Ctrl+C stops all three apps. Set STORE_VERBOSE=1 for worker logs.');
  for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => { running.stop(); process.exit(); });
}
