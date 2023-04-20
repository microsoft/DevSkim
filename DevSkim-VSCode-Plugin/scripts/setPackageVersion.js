import * as nbgv from 'nerdbank-gitversioning';
import * as cp from 'child_process';

const versionInfo = await nbgv.getVersion();
console.log(`Setting package version to ${versionInfo.simpleVersion}`);
cp.execSync(`npm version ${versionInfo.simpleVersion}`);