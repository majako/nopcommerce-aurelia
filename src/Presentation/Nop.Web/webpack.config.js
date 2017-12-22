const path = require('path');
const { AureliaPlugin } = require('aurelia-webpack-plugin');
const { ProvidePlugin } = require('webpack');
const { TsConfigPathsPlugin, CheckerPlugin } = require('awesome-typescript-loader');

const outDir = path.resolve(__dirname, 'wwwroot/dist');
const srcDir = path.resolve(__dirname, 'wwwroot/aurelia');
const nodeModulesDir = path.resolve(__dirname, 'node_modules');

/**
 * @return {webpack.Configuration}
 */
module.exports = ({ production } = {}) => ({
    resolve: {
        extensions: ['.ts', '.js'],
        modules: [srcDir, nodeModulesDir],
    },
    devtool: production ? 'source-map' : 'cheap-module-eval-source-map',
    entry: {
        app: ['aurelia-bootstrapper'],
        vendor: ['bluebird'],
    },
    output: {
        path: outDir,
        filename: '[name].bundle.js',
        sourceMapFilename: '[name].bundle.map'
    },
    module: {
        rules: [
            { test: /\.html$/i, loader: 'html-loader' },
            { test: /\.ts$/i, loader: 'awesome-typescript-loader', exclude: nodeModulesDir },
            // use Bluebird as the global Promise implementation:
            { test: /[\/\\]node_modules[\/\\]bluebird[\/\\].+\.js$/, loader: 'expose-loader?Promise' }
        ]
    },
    plugins: [
        new AureliaPlugin({ aureliaApp: 'main' }),
        new ProvidePlugin({
            'Promise': 'bluebird'
        }),
        new TsConfigPathsPlugin(),
        new CheckerPlugin()
    ],
})