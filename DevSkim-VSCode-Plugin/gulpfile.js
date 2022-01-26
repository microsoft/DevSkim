var gulp = require('gulp');
var nbgv = require('nerdbank-gitversioning');

gulp.task('setPackageVersion', () => nbgv.setPackageVersion());

gulp.task('resetPackageVersion', () => nbgv.resetPackageVersionPlaceholder());