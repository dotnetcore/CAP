<template>
  <div>
    <h2 class="text-left mb-4">{{ $t("Nodes") }}</h2>
    <b-table :fields="fields" :items="items" :busy="isBusy" small fixed head-variant="light" show-empty empty-text="Unconfigure node discovery !">
      <template #table-busy>
        <div class="text-center text-secondary my-2">
          <b-spinner class="align-middle"></b-spinner>
          <strong class="ml-2">{{ $t("Loading") }}...</strong>
        </div>
      </template>

      <template #empty="scope">
        <h5 class="alert alert-info" role="alert">
          <b-icon-info-circle-fill /> {{ scope.emptyText }}
        </h5>
      </template>

      <template #cell(actions)="data">
        <b-button size="sm" @click="switchNode(data.item)" class="mr-1">
          {{ $t("Switch") }}
        </b-button>
      </template>
    </b-table>
  </div>
</template>
<script>
import axios from 'axios';
import { BIconInfoCircleFill } from 'bootstrap-vue';

export default {
  components: {
    BIconInfoCircleFill
  },
  data() {
    return {
      isBusy: false,
      items: []
    }
  },
  computed: {
    fields() {
      return [
        { key: "id", label: this.$t("Id") },
        { key: "name", label: this.$t("Node Name") },
        { key: "address", label: this.$t("Ip Address") },
        { key: "port", label: this.$t("Port") },
        { key: "tags", label: this.$t("Tags") },
        { key: "actions", label: this.$t("Actions") },
      ];
    }
  },
  mounted() {
    this.fetchData()
  },
  methods: {
    fetchData() {
      this.isBusy = true;
      var name = this.getCookie('cap.node');
      axios.get('/nodes').then(res => {
        for (var item of res.data) {
          if (item.name == name) {
            item._rowVariant = 'dark'
          }
        }
        this.items = res.data;
        this.isBusy = false;
      });
    },

    switchNode(item) {
      document.cookie = `cap.node=${escape(item.name)};`;
      window.location.reload();
    },

    getCookie(cname) {
      var name = cname + "=";
      var decodedCookie = decodeURIComponent(document.cookie);
      var ca = decodedCookie.split(';');
      for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
          c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
          return c.substring(name.length, c.length);
        }
      }
      return "";
    }
  }

};
</script>
<style >
.table-dark td {
  border-color: #c6c8ca;
}
</style>