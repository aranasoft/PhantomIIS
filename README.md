# Phantom IIS

[PhantomIIS](http://github.com/jayharris/phantomiis) is utility for executing for
[PhantomJS](http://phantomjs.org/) within the context of an IIS Express web server.

## Why?

Unlike compile-time bundling and minification utilities like GruntJS, many ASP.NET applications
rely on the [Web Optimization Framework](http://aspnetoptimization.codeplex.com/) to perform run-time
bundling and minification on the server. In these cases, it is often helpful and necessary to execute
JavaScript tests using the ASP.NET bundler. By executing under IIS Express, PhantomJS can have full
access to the ASP.NET site, and it's pages, scripts, and bundles, without the developer having to
fully configure an IIS site.

## Install

PhantomIIS will be available for download soon. Stay tuned. In the mean time, clone the repository and compile.

## Usage

To execute PhantomIIS from the command line:

    phantomiis

### --iisexpress=VALUE, -i

The path to `iisexpress.exe`. This is usually `C:\Program Files\IIS Express\iisexpress.exe` or `C:\Program Files (x86)\IIS Express\iisexpress.exe`. If you do not specify this flag, PhatomIIS will search your PATH.

### --phantomjs=VALUE, -j

The path to `phantomjs.exe`. If you do not specify this flag, PhatomIIS will search your PATH environment variable.

### --phantomconfig=VALUE, -jc

The path to the PhantomJS JSON Configuration file. This configuration file is passed to PhantomJS's `--config` flag, and is used instead of exposing all PhantomJS CLI flags through PhantomIIS. If this flag is not specified, then the default PhantomJS configuration is used.

### --phantomscript=VALUE, -js

The path to the PhantomJS execution script. Default: `.\phantom.run.js`.

### --siteroot=VALUE, -s

The path to the root of your ASP.NET Web Site. Default: `.\`

### --port=VALUE, -p

The port to assign to your ASP.NET Web Site. Default: 3000

### --help, -h, -?

Display help text

### --version, -V

Display the current version of PhantomIIS

## Hat Tips

 - [@roysvork](https://github.com/Roysvork) - This is largely based on and inspired by Pete's [PhantomExpress Gist](https://gist.github.com/Roysvork/5274142).
 - [@harveykwok](http://stackoverflow.com/users/452199/harvey-kwok) - [How to programmatically stop IIS Express](http://stackoverflow.com/questions/4772092/starting-and-stopping-iis-express-programmatically/4777927#4777927) without `process.kill()`

## License

PhantomIIS is copyright of Arana Software, released under the [BSD License](http://opensource.org/licenses/BSD-3-Clause).