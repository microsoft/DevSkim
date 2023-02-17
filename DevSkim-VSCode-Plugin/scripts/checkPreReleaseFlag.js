import * as nbgv from 'nerdbank-gitversioning';

var versionInfo = await nbgv.getVersion();
const version = versionInfo.assemblyInformationalVersion;

if (version.indexOf("alpha") > -1 ) {
	console.log("alpha version");
} 
else {
	console.log("not alpha version");
}