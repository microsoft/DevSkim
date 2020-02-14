var gulp = require('gulp');
var nbgv = require('nerdbank-gitversioning')
const fs = require('fs');
const util = require('util');
const renameAsync = util.promisify(fs.rename);
const { exec } = require('child_process');

gulp.task('setPackageVersion', function(done) {
    const gitVersion = nbgv.getVersion();
    exec(`npm version ${gitVersion.npmPackageVersion} --no-git-tag-version --allow-same-version`);
    done();
});

gulp.task('resetPackageVersion', function(done){
    exec(`npm version 0.0.0-placeholder --no-git-tag-version --allow-same-version`);
    done();
});

gulp.task('default', gulp.series('setPackageVersion'));