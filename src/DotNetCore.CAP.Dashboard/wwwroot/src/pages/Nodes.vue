<template>
  <div>
    <h2 class="text-left mb-4">{{ $t("Nodes") }}</h2>

    <b-row class="mb-3" v-if="nsList.length > 0">
      <b-col>
        <b-form-select v-model="selected" value-field="item" text-field="name" @change="fetchSvcs()" :options="nsList">
          <template #first>
            <b-form-select-option :value="null" disabled>{{ $t("SelectNamespaces") }}</b-form-select-option>
          </template>
        </b-form-select>
      </b-col>
      <b-col cols="1">
        <b-button variant="dark" :disabled="selected == null || pinging" aria-disabled="true" id="latency"
          @click="pingSvcs()">
          <b-icon-speedometer2 v-if="!pinging"></b-icon-speedometer2>
          <b-icon-arrow-clockwise v-if="pinging" animation="spin"></b-icon-arrow-clockwise>
        </b-button>
      </b-col>
    </b-row>

    <b-table :fields="fields" :items="items" :busy="isBusy" small head-variant="light" show-empty striped hover responsive
      thead-tr-class="text-left" :empty-text="nsList.length == 0 ? $t('NonDiscovery') : $t('EmptyRecords')">

      <template #table-colgroup="scope">
        <col v-for="field in scope.fields" :key="field.key" :style="{ width: colWidth(field.key) }">
      </template>

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

      <template #cell(id)="data">
        <div class="texts">
          {{ data.item.id }}
        </div>
      </template>

      <template #cell(address)="data">
        {{ data.item.address + (data.item.port == 80 ? "" : ":" + data.item.port) }}
      </template>

      <template #cell(tags)="data">
        <b-badge variant="info">{{ data.item.tags.split(',')[0] }}</b-badge>
        <b-link @click="data.toggleDetails" v-if="data.item.tags != ''">
          <div class="ml-2" style="font-size:12px;color:gray">Show {{ data.detailsShowing ? 'Less' : 'More' }} </div>
        </b-link>
      </template>

      <template #row-details="data">
        <b-badge class="mb-1 ml-2" v-for="tag in data.item.tags.split(',')" :key="tag">{{ tag }}</b-badge>
      </template>

      <template #cell(latency)="data">
        <div v-if="data.item.latency == null"></div>
        <b-badge v-else-if="(typeof data.item.latency === 'number')" variant="success">
          {{ data.item.latency + " ms" }}
        </b-badge>
        <b-badge pill href="#" v-else v-b-popover.hover.left="data.item.latency" variant="danger" size="sm"
          title="Request failed">
          Error
        </b-badge>
      </template>

      <template #cell(actions)="data">
        <b-button size="sm" variant="dark" @click="switchNode(data.item);">
          <b-spinner small variant="secondary" v-if="data.item._ping" type="grow" label="Spinning"></b-spinner>
          {{ $t("Switch") }}
        </b-button>
      </template>
    </b-table>
  </div>
</template>
<script>
import axios from 'axios';
import { BIconInfoCircleFill, BIconSpeedometer2, BIconArrowClockwise, BIconSearch } from 'bootstrap-vue';

var cancelToken = axios.CancelToken.source();

export default {
  components: {
    BIconInfoCircleFill,
    BIconSpeedometer2,
    BIconArrowClockwise,
    BIconSearch
  },
  data() {
    return {
      pinging: false,
      selected: null,
      nsList: [],
      isBusy: false,
      items: []
    }
  },
  computed: {
    fields() {
      return [
        { key: "id", label: this.$t("Id") },
        { key: "name", label: this.$t("Node Name"), tdClass: "text-left" },
        { key: "address", label: this.$t("Ip Address"), tdClass: "text-left" },
        { key: "tags", label: this.$t("Tags"), tdClass: "text-left" },
        { key: "latency", label: this.$t("Latency"), thClass: "text-center", tdClass: "text-success" },
        { key: "actions", label: this.$t("Actions"), thClass: "text-center" },
      ];
    }
  },
  mounted() {
    this.fetchNsOptions();
    this.fetchData();
  },
  methods: {
    colWidth(key) {
      switch (key) {
        case "address":
          return "320px";
        case "actions":
          return "80px";
        case "latency":
          return "60px";
        default:
          return "";
      }
    },
    fetchNsOptions() {
      this.isBusy = true;
      axios.get('/list-ns').then(res => {
        if (res.data.length > 0) {
          this.nsList = res.data;
        }
      });
      this.isBusy = false;
      var ns = this.getCookie("cap.node.ns");
      if (ns) {
        this.selected = ns;
        this.fetchSvcs();
      }
    },

    fetchSvcs() {
      if (!this.selected) return;

      this.isBusy = true;
      var name = this.getCookie('cap.node');
      if (this.pinging == true) {
        cancelToken.cancel();
        cancelToken = axios.CancelToken.source();
      }
      axios.get('/list-svc/' + this.selected).then(res => {
        for (var item of res.data) {
          if (item.name == name) {
            item._rowVariant = 'dark'
          }
          item._ping = false; //add new property
        }
        this.items = res.data;
        this.isBusy = false;
      });

    },

    async pingSvcs() {
      this.pinging = true;
      for (var item of this.items) {
        try {
          var res = await axios.get('/ping', {
            params: {
              endpoint: item.address + ":" + item.port
            },
            timeout: 3000,
            cancelToken: cancelToken.token
          });
          item.latency = res.data;
        } catch (err) {
          if (axios.isCancel(err)) break;
          item.latency = err.response?.data;
        }
      }
      this.pinging = false;
    },

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
      item._ping = true;
      axios.get('/ping', {
        params: {
          endpoint: item.address + ":" + item.port
        },
        timeout: 3000
      }).then(res => {
        item.latency = res.data;
        document.cookie = `cap.node=${escape(item.name)};`;
        document.cookie = `cap.node.ns=${this.selected};`;
        item._ping = false;
        location.reload();
      }).catch(err => {
        if (axios.isAxiosError(err)) {
          item.latency = err.response?.data;
          this.$bvToast.toast("Switch to [" + item.name + "] failed! Endpoint: " + item.address, {
            title: "Warning",
            variant: "danger",
            autoHideDelay: 2000,
            appendToast: true,
            solid: true
          });
        }
        item._ping = false;
      });
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

.texts {
  width: 70px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
</style>