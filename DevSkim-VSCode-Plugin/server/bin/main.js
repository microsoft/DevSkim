#!/usr/bin/env node


//not sure when this was added, but it looks like a stab at a CLI
//however it doesn't use most of the NPM conventions of one, so should
//probably be drastically overhauled to do so



const server = require('../out/index');

const args = process.argv;
const start = args.find(s => s === 'start');
const version = args.find(s => s === '-v' || s === '--version');
const help = args.find(s => s === '-h' || s === '--help');

// console.log(`main: starting args(${args})`);

if (start) {
    server.listen()
} else if (version) {
    // console.log(`Version is ${pkg.version}`)
} else if (help) {
    console.log(`
Usage:
  devskim-language-server start
  devskim-language-server -h | --help
  devskim-language-server -v | --version
  `)
} 
/*
else 
{
    const command = args.join(' ')
    // console.error(`Unknown command '${command}'. Run with -h for help.`)
}
*/