import Vue from 'vue'
import App from './App.vue'
import router from './router'
import baseURL from './global'
import { BootstrapVue, IconsPlugin } from 'bootstrap-vue'
import VueJsonPretty from 'vue-json-pretty';
import 'vue-json-pretty/lib/styles.css';
import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-vue/dist/bootstrap-vue.css'
import store from '@/store/store.js'
import axios from "axios";
import VueI18n from 'vue-i18n'
//
//
axios.defaults.baseURL = baseURL;
axios.defaults.withCredentials = true
axios.defaults.headers.post['Content-Type'] = 'application/json';
axios.interceptors.request.use(
  config => {
    let accessToken = localStorage.getItem('token');
    if (accessToken) {
      config.headers = Object.assign({
        Authorization: `Bearer ${accessToken}`
      }, config.headers);
    }
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

Vue.config.productionTip = false

// Make BootstrapVue available throughout your project
Vue.use(BootstrapVue)
Vue.use(IconsPlugin)
Vue.component("vue-json-pretty", VueJsonPretty)
Vue.use(VueI18n)

const i18n = new VueI18n({
    locale:(function(){
        if(localStorage.getItem('lang')){
            return localStorage.getItem('lang')
        }
        return 'en-us'
    }()),
    messages:{
        'en-us':require('./assets/language/en-us'), //英文语言包
        'zh-cn':require('./assets/language/zh-cn'), //中文繁体包
    }
})

new Vue({
  router,
  store,
    i18n,
  render: h => h(App)
}).$mount('#app')
