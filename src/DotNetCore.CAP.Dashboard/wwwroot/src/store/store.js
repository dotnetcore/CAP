import Vue from 'vue';
import Vuex from 'vuex';

Vue.use(Vuex);

let store = new Vuex.Store({ 
    state: {
        metric: {},
        info: {}
    },

    getters: {
        getMetric(state) {
            return state.metric;
        }
    },
    mutations: {
        setMertic(state, val) {
            state.metric = val;
        },
        setInfo(state, val){
            state.info = val;
        }
    },
    actions: {
        pollingMertic({ commit }, val) {
            commit("setMertic", val);
        },
        pollingInfo({ commit }, val) {
            commit("setInfo", val);
        }
    },
});

export default store;