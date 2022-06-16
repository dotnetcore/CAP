<template>
  <div>
    <b-navbar toggleable="lg" type="dark" sticky variant="secondary">
      <b-container>
        <b-navbar-brand to="/">{{ $t(brandTitle) }}</b-navbar-brand>
        <b-navbar-toggle target="nav-collapse"></b-navbar-toggle>

        <b-collapse id="nav-collapse" is-nav>
          <b-navbar-nav>
            <b-nav-item v-for="menu in menus" :to="menu.path" :key="menu.name" active-class="active">
              {{ $t(menu.name) }}
              <b-badge :variant="menu.variant" v-if="onMetric[menu.badge]"> {{ onMetric[menu.badge] }}</b-badge>
            </b-nav-item>
          </b-navbar-nav>
        </b-collapse>

        <b-navbar-nav class="ml-auto">
          <b-nav-item>
            <b-dropdown size="sm" id="dlLang" :text="$t('LanguageName')" >
              <b-dropdown-item v-for="lang in languages" :key="lang.code" :active="checkCurrentLang(lang.code)" @click="changeLang(lang.code)">{{ lang.name }}</b-dropdown-item>
            </b-dropdown>
          </b-nav-item>

          <b-nav-item href="https://github.com/dotnetcore/CAP" target="_blank" :link-attrs="{ 'aria-label': 'GitHub' }">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" width="24" height="24" class="navbar-nav-svg"
                 focusable="false" role="img">
              <title>GitHub</title>
              <g fill="currentColor">
                <path fill-rule="evenodd" clip-rule="evenodd"
                      d="M16,0.4c-8.8,0-16,7.2-16,16c0,7.1,4.6,13.1,10.9,15.2 c0.8,0.1,1.1-0.3,1.1-0.8c0-0.4,0-1.4,0-2.7c-4.5,1-5.4-2.1-5.4-2.1c-0.7-1.8-1.8-2.3-1.8-2.3c-1.5-1,0.1-1,0.1-1 c1.6,0.1,2.5,1.6,2.5,1.6c1.4,2.4,3.7,1.7,4.7,1.3c0.1-1,0.6-1.7,1-2.1c-3.6-0.4-7.3-1.8-7.3-7.9c0-1.7,0.6-3.2,1.6-4.3 c-0.2-0.4-0.7-2,0.2-4.2c0,0,1.3-0.4,4.4,1.6c1.3-0.4,2.6-0.5,4-0.5c1.4,0,2.7,0.2,4,0.5C23.1,6.6,24.4,7,24.4,7 c0.9,2.2,0.3,3.8,0.2,4.2c1,1.1,1.6,2.5,1.6,4.3c0,6.1-3.7,7.5-7.3,7.9c0.6,0.5,1.1,1.5,1.1,3c0,2.1,0,3.9,0,4.4 c0,0.4,0.3,0.9,1.1,0.8C27.4,29.5,32,23.5,32,16.4C32,7.6,24.8,0.4,16,0.4z"/>
              </g>
            </svg>
          </b-nav-item>
        </b-navbar-nav>
      </b-container>
    </b-navbar>
  </div>
</template>
<script>
export default {
  name: "Navigation",
  computed: {
    onMetric() {
      return this.$store.getters.getMetric;
    }
  },
  methods:{
    changeLang(langCode){
      localStorage.setItem('lang',langCode);
      this.$i18n.locale=langCode;
    },
    checkCurrentLang(langCode){
      return this.$i18n.locale==langCode;
    }
  },
  data() {
    return {
      i18nPrefix: "_.Navigation.",
      brandTitle: "CAP Dashboard",
      languages: [
        {name: "English",code:"en-us", active: true },
        {name: "简体中文", code:"zh-cn",active:false}
      ],
      menus: [
        {name: "Published", path: "/published", variant: "danger", badge: "publishedFailed"},
        {name: "Received", path: "/received", variant: "danger", badge: "receivedFailed"},
        {name: "Subscriber", path: "/subscriber", variant: "info", badge: "subscribers"},
        {name: "Nodes", path: "/nodes", variant: "light", badge: "servers"},
      ]
    };
  },

};
</script>
<style scoped>
.nav-item {
  padding: 0 10px;
}
</style>