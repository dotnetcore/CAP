import Vue from 'vue'
import App from './App.vue'
import router from './router'

import { BootstrapVue, IconsPlugin } from 'bootstrap-vue'
import VueJsonPretty from 'vue-json-pretty';
import 'vue-json-pretty/lib/styles.css';
import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-vue/dist/bootstrap-vue.css'
import store from '@/store/store.js'
import axios from "axios";

axios.defaults.baseURL = window.serverUrl;
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

new Vue({
  router,
  store,
  render: h => h(App)
}).$mount('#app')
