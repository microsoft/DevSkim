var gulp = require('gulp');
var nbgv = require('nerdbank-gitversioning')
const fs = require('fs');
const util = require('util');
const renameAsync = util.promisify(fs.rename);
const { exec } = require('child_process');

gulp.task('setPackageVersion', async function(done) {
    const gitVersion = await nbgv.getVersion();    
    exec(`npm version ${gitVersion.simpleVersion} --no-git-tag-version --allow-same-version`);
    done();
});

gulp.task('resetPackageVersion', function(done){
    exec(`npm version 0.0.0-placeholder --no-git-tag-version --allow-same-version`);
    done();
});

gulp.task('default', gulp.series('setPackageVersion'));