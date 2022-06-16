<template>
  <div>
    <h1 class="text-left mb-4">{{$t("Nodes")}}</h1>
    <b-table :fields="fields" :items="items" :busy="isBusy" show-empty empty-text="Unconfigure node discovery !">
      <template #table-busy>
        <div class="text-center text-secondary my-2">
          <b-spinner class="align-middle"></b-spinner>
          <strong class="ml-2">{{$t("Loading")}}...</strong>
        </div>
      </template>

      <template #empty="scope">
        <h4 class="alert alert-primary" role="alert">
          <b-icon-info-circle-fill /> {{ scope.emptyText }}
        </h4>
      </template>

      <template #cell(actions)="data">
        <b-button size="sm" @click="switchNode(data.item)" class="mr-1">
          Switch
        </b-button>
      </template>
    </b-table>
  </div>
</template>
<script>
import axios from 'axios';
export default {
  data() {
    return {
      isBusy: false,
      items: []
    }
  },
  computed:{
    fields(){
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
      var id = this.getCookie('cap.node');
      axios.get('/nodes').then(res => {
        for (var item of res.data) {
          if (item.id == id) {
            item._rowVariant = 'primary'
          }
        }
        this.items = res.data;
        this.isBusy = false;
      });
    },

    switchNode(item) {
      document.cookie = `cap.node=${escape(item.id)};`;
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