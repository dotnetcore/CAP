import Vue from 'vue'
import VueRouter from 'vue-router'
import Home from '../pages/Home.vue'

Vue.use(VueRouter)


const routes = [
    {
        path: '/',
        name: 'Home',
        component: Home
    },
    {
        path: '/published/:status',
        name: 'Published',
        props: true,
        component: () => import('../pages/Published.vue')
    },
    {
        path: '/published',
        redirect: '/published/succeeded'
    },
    {
        path: '/received/:status',
        name: 'Received',
        props: true,
        component: () => import('../pages/Received.vue')
    },
    {
        path: '/received',
        redirect: '/received/succeeded'
    },
    {
        path: '/subscriber',
        name: 'Subscriber',
        component: () => import('../pages/Subscriber.vue')
    },
    {
        path: '/nodes',
        name: 'Nodes',
        component: () => import('../pages/Nodes.vue')
    }
]

const router = new VueRouter({
    routes
})

export default router;