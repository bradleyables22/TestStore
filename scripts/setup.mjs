import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
for (const name of ['AppHost', 'Catalog', 'Cart']) {
    const directory = path.join(root, `TestStore.${name}`);
    const target = path.join(directory, 'local.settings.json');
    if (fs.existsSync(target)) { console.log(`Keeping TestStore.${name}/local.settings.json`); continue; }
    fs.copyFileSync(path.join(directory, 'local.settings.example.json'), target, fs.constants.COPYFILE_EXCL);
    console.log(`Created TestStore.${name}/local.settings.json`);
}
