<template>
  <b-navbar fixed="bottom" toggleable="lg" type="light" variant="light">
    <b-container class="d-block">
      <b-row class="align-items-start">
        <b-col v-if="nodeName">
          <BBadge variant="warning" pill> {{$t("SwitchedNode")}}: {{ nodeName }}</BBadge>
        </b-col>
        <b-col>
          <BBadge variant="secondary" pill>{{ meta.cap?.name }}: {{ meta.cap?.version.substring(0, 5) }}</BBadge>
        </b-col>
        <b-col>
          <BBadge variant="secondary" pill> {{$t("Storage")}}: {{ meta.storage?.name }}</BBadge>
        </b-col>
        <b-col>
          <BBadge variant="secondary" pill> {{$t("Transport")}}: {{ meta.broker?.name }}</BBadge>
        </b-col>
      </b-row>
    </b-container>
  </b-navbar>
</template>
<script>
import axios from 'axios';
import { BBadge } from 'bootstrap-vue';
export default {
  data() {
    return {
      nodeName: "",
      meta: {}
    };
  },
  async mounted() {
    this.nodeName = this.getCookie("cap.node");
    await axios.get("/meta").then(res => {
      this.meta = res.data;
    });
  },
  methods: {
    getCookie(cname) {
      var name = cname + "=";
      var decodedCookie = decodeURIComponent(document.cookie);
      var ca = decodedCookie.split(";");
      for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == " ") {
          c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
          return c.substring(name.length, c.length);
        }
      }
      return "";
    }
  },
  components: { BBadge }
}
</script>
<style scoped>.d-block {
  color: rgba(255, 255, 255, 0.6);
}</style>