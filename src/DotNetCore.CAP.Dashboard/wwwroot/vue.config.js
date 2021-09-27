module.exports = {
    publicPath: './',
    productionSourceMap: false,
    chainWebpack: config => {
        config.plugins.delete('prefetch')
    }
}