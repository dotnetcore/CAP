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

        <b-navbar-nav class="ml-auto ">
          <b-nav-item>
            <b-dropdown size="sm" id="dlLang" text="secondary" variant="secondary" :text="$t('LanguageName')">
              <b-dropdown-item v-for="lang in languages" class="text-secondary" :key="lang.code"
                :active="checkCurrentLang(lang.code)" @click="changeLang(lang.code)">{{ lang.name }}</b-dropdown-item>
            </b-dropdown>
          </b-nav-item>

          <b-nav-item href="https://github.com/dotnetcore/CAP" target="_blank" title="Github">
            <b-icon-github style="width:24px;height:24px"></b-icon-github>
          </b-nav-item>
        </b-navbar-nav>
      </b-container>
    </b-navbar>
  </div>
</template>
<script>
import { BIconGithub } from 'bootstrap-vue';
export default {
  name: "Navigation",
  components: {
    BIconGithub
  },
  computed: {
    onMetric() {
      return this.$store.getters.getMetric;
    }
  },
  methods: {
    changeLang(langCode) {
      localStorage.setItem('lang', langCode);
      this.$i18n.locale = langCode;
    },
    checkCurrentLang(langCode) {
      return this.$i18n.locale == langCode;
    }
  },
  data() {
    return {
      i18nPrefix: "_.Navigation.",
      brandTitle: "CAP Dashboard",
      languages: [
        { name: "English", code: "en-us", active: true },
        { name: "简体中文", code: "zh-cn", active: false }
      ],
      menus: [
        { name: "Published", path: "/published", variant: "danger", badge: "publishedFailed" },
        { name: "Received", path: "/received", variant: "danger", badge: "receivedFailed" },
        { name: "Subscriber", path: "/subscriber", variant: "info", badge: "subscribers" },
        { name: "Nodes", path: "/nodes", variant: "light", badge: "servers" },
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