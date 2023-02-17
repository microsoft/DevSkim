import * as nbgv from 'nerdbank-gitversioning';
import { exit } from 'process';

async function isPreRelease() {
	var versionInfo = await nbgv.getVersion();
	const prereleaseTag = versionInfo.prereleaseVersionNoLeadingHyphen;
	if (!prereleaseTag) {
		return false;
	} 
	return true;
};

if (await isPreRelease())
{
	console.log("Building Pre-Release");
	exit(0);
}
else{
	console.log("Building Release");
	exit(-1);
}