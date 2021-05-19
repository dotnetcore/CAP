module.exports = {
    publicPath: './',
    chainWebpack: config => {
        config.plugins.delete('prefetch')
    }
}