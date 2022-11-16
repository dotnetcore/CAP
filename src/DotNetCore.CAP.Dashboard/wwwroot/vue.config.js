module.exports = {
    publicPath: './',
    productionSourceMap: false,
    chainWebpack: config => {
        config.plugins.delete('prefetch')
    },
    devServer: {
        proxy: {
            '^/cap/api': {
                target: 'http://localhost:5000',
                changeOrigin: true
            }
        }
    }
}