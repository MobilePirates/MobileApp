/// <binding BeforeBuild='default, bower, copy, less, tsd, ts' />

/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    grunt.initConfig({
        bower: {
            install: {
                options: {
                    targetDir: "www/libs",
                    layout: "byComponent",
                    cleanTargetDir: false
                }
            }
        },
        tsd: {
            refresh: {
                options: {
                    // execute a command
                    command: 'reinstall',

                    //optional: always get from HEAD
                    latest: true,

                    // specify config file
                    config: 'tsd.json',

                    // experimental: options to pass to tsd.API
                    opts: {
                        // props from tsd.Options
                    }
                }
            }
        },
        ts: {
            app: {
                options: {
                    fast: 'never'
                },
                files: [
                    {
                        src: "assets/scripts/**/*.ts",
                        dest: "www/scripts/app.js"
                    
                }]
            }
        },
        less: {
            app: {
                files: {
                    'www/css/app.css': 'assets/less/**/*.less'
                }
            }
        },
        copy: {
            all: {
                files: {
                    'www/index.html': 'assets/index.html'
                }
            }
        }
    });

    grunt.registerTask("default", ["bower:install", 'tsd:refresh', 'ts:app', 'less:app', 'copy:all']);

    grunt.loadNpmTasks("grunt-bower-task");
    grunt.loadNpmTasks("grunt-ts");
    grunt.loadNpmTasks("grunt-tsd");
    grunt.loadNpmTasks("grunt-contrib-less");
    grunt.loadNpmTasks("grunt-copy");
};