var gulp = require('gulp');
var nbgv = require('nerdbank-gitversioning')

const fs = require('fs');
const util = require('util');

gulp.task('setPackageVersion', function() {
    const renameAsync = util.promisify(fs.rename);

    return renameAsync('package-lock.json', 'package-lock.backup').then(function() {
        return nbgv.setPackageVersion().finally(function() {
            return renameAsync('package-lock.backup', 'package-lock.json');
        });
    });
});