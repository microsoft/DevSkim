// rewrite-registry.mjs
import { readFile, writeFile } from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

// Support __dirname in ES modules
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const filePath = process.argv[2];
const newRegistryBase = process.argv[3];

if (!filePath || !newRegistryBase) {
  console.error('❌ Please provide the path to the package-lock.json file and the new registry base URL.');
  console.error('Usage: node updatePackageLock.js <path-to-package-lock.json> <new-registry-base-url>');
  process.exit(1);
}

const registryBase = 'https://registry.npmjs.org/';

try {
  const fullPath = path.resolve(__dirname, filePath);
  const content = await readFile(fullPath, 'utf8');
  const lock = JSON.parse(content);

  if (lock.packages) {
    for (const [pkg, data] of Object.entries(lock.packages)) {
      if (data.resolved?.startsWith(registryBase)) {
        data.resolved = newRegistryBase + data.resolved.slice(registryBase.length);
      }
    }
    await writeFile(fullPath, JSON.stringify(lock, null, 2));
    console.log(`✅ Updated resolved URLs in ${filePath}`);
  } else {
    console.error('⚠️ No "packages" field found in the file.');
  }
} catch (err) {
  console.error(`❌ Failed to process ${filePath}:`, err.message);
}
