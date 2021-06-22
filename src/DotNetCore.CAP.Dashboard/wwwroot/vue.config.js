module.exports = {
    productionSourceMap: false,
    publicPath: './',
    chainWebpack: config => {
        config.plugins.delete('prefetch')
    }
}