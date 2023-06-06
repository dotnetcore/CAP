<template>
  <div>
    <b-row>
      <b-col md="3">

        <b-list-group>
          <b-tooltip target="tooltip" triggers="hover" variant="warning" custom-class="my-tooltip-class"
            placement="bottomright">
            {{ $t("DelayedInfo") }}
          </b-tooltip>
          <router-link class="list-group-item text-left list-group-item-secondary list-group-item-action"
            v-for="(menu, index) of subMens" :key="menu.text" active-class="active" :to="menu.name">
            {{ $t(menu.text) }}
            <b-icon-info-circle-fill id="tooltip" v-if="index == subMens.length - 1">
            </b-icon-info-circle-fill>
            <b-badge :variant="menu.variant" class="float-right" pill> {{ onMetric[menu.num] }} </b-badge>
          </router-link>
        </b-list-group>
      </b-col>

      <b-col md="9">
        <h2 class="page-line mb-3">{{ $t("Published Message") }}</h2>

        <b-form class="d-flex">
          <div class="col-sm-10">
            <div class="form-row mb-2">
              <label for="form-input-name" class="sr-only">{{ $t("Name") }}</label>
              <b-form-input v-model="formData.name" id="form-input-name" class="form-control" :placeholder="$t('Name')" />
            </div>
            <div class="form-row">
              <label class="sr-only" for="inline-form-input-content">{{ $t("Content") }}</label>
              <b-form-input v-model="formData.content" id="inline-form-input-content" class="form-control"
                :placeholder="$t('Content')" />
            </div>
          </div>
          <div class="align-self-end">
            <b-button variant="dark" @click="onSearch">
              <b-icon-search></b-icon-search>
              {{ $t("Search") }}
            </b-button>
          </div>
        </b-form>
      </b-col>
    </b-row>

    <b-row>
      <b-col md="12">
        <b-btn-toolbar class="mt-4">
          <b-button size="sm" variant="dark" @click="requeue" :disabled="!selectedItems.length">
            <b-icon-arrow-repeat aria-hidden="true"></b-icon-arrow-repeat>
            {{ requeueTitle }}
          </b-button>
          <div class="pagination">
            <span style="font-size: 14px">{{ $t("Page Size") }}:</span>
            <b-button-group class="ml-2">
              <b-button variant="outline-secondary" size="sm" v-for="size in pageOptions"
                :class="{ active: formData.perPage == size }" @click="pageSizeChange(size)" :key="size">{{ size }}
              </b-button>
            </b-button-group>
          </div>
        </b-btn-toolbar>
        <b-table id="datatable" :busy="isBusy" class="mt-3" striped thead-tr-class="text-left " head-variant="light"
          details-td-class="align-middle" tbody-tr-class="text-left" small :fields="fields" :items="items"
          select-mode="range">
          <template #table-busy>
            <div class="text-center text-secondary my-2">
              <b-spinner class="align-middle"></b-spinner>
              <strong class="ml-2">{{ $t("Loading") }}...</strong>
            </div>
          </template>

          <template #head(checkbox)="">
            <b-form-checkbox @change="selectAll" v-model="isSelectedAll"></b-form-checkbox>
          </template>

          <template #cell(checkbox)="data">
            <b-form-checkbox v-model="data.item.selected" @change="select(data.item)">
            </b-form-checkbox>
          </template>

          <template #cell(id)="data">
            <b-link @click="info(data.item, $event.target)">
              {{ data.item.id }}
            </b-link>
            <br />
            {{ data.item.name }}
          </template>

        </b-table>
        <span class="float-left"> {{ $t("Total") }}: {{ totals }} </span>
        <b-pagination :first-text="$t('First')" :prev-text="$t('Prev')" :next-text="$t('Next')" :last-text="$t('Last')"
          v-model="formData.currentPage" :total-rows="totals" :per-page="formData.perPage" class="capPagination"
          aria-controls="datatable"></b-pagination>
      </b-col>
    </b-row>
    <b-modal size="lg" :id="infoModal.id" :title="'Id: ' + infoModal.title" ok-only ok-variant="secondary">
      <vue-json-pretty showSelectController :key="infoModal.id" :data="infoModal.content" />
    </b-modal>
  </div>
</template>
<script>
import axios from "axios";
import JSONBIG from "json-bigint";
import {
  BIconInfoCircleFill,
  BIconArrowRepeat,
  BIconSearch
} from 'bootstrap-vue';

const formDataTpl = {
  currentPage: 1,
  perPage: 10,
  name: "",
  content: "",
};
export default {
  components: {
    BIconInfoCircleFill,
    BIconArrowRepeat,
    BIconSearch
  },
  props: {
    status: {}
  },
  data() {
    return {
      subMens: [
        {
          variant: "secondary",
          text: "Succeeded",
          num: 'publishedSucceeded',
          name: "/published/succeeded",
        },
        {
          variant: "danger",
          text: "Failed",
          name: "/published/failed",
          num: 'publishedFailed',
        },

        {
          variant: "warning",
          text: "Delayed",
          name: "/published/delayed",
          num: 'publishedDelayed',
        },
      ],
      pageOptions: [10, 20, 50, 100, 500],
      selectedItems: [],
      isBusy: false,
      tableValues: [],
      isSelectedAll: false,
      formData: { ...formDataTpl },
      totals: 0,
      items: [],
      infoModal: {
        id: "info-modal",
        title: "",
        content: "{}",
      },
      expiresTitle: this.$t("Expires"),
      requeueTitle: this.$t("Requeue")
    };
  },
  computed: {
    onMetric() {
      return this.$store.getters.getMetric;
    },
    fields() {
      return [{ key: "checkbox", label: "" },
      { key: "id", label: this.$t("IdName") },
      { key: "retries", label: this.$t("Retries") },
      {
        key: "added",
        label: this.$t("Added"),
        formatter: (val) => {
          if (val != null) return new Date(val).format("yyyy-MM-dd hh:mm:ss");
        },
      },
      {
        key: "expiresAt",
        label: this.expiresTitle,
        formatter: (val) => {
          if (val != null) return new Date(val).format("yyyy-MM-dd hh:mm:ss");
        },
      }];
    }
  },
  mounted() {
    this.fetchData();
    window.abc = this;
  },
  watch: {
    status: function () {
      this.fetchData();
    },
    "formData.currentPage": function () {
      this.fetchData();
    },
  },
  methods: {
    fetchData() {
      this.isBusy = true;
      axios.get(`/published/${this.status}`, {
        params: this.formData
      }).then(res => {
        this.items = res.data.items;
        this.totals = res.data.totals;
        if (this.status == "delayed") {
          this.expiresTitle = this.$t("DelayedPublishTime");
          this.requeueTitle = this.$t("PublishNow")
        } else {
          this.expiresTitle = this.$t("Expires");
          this.requeueTitle = this.$t("Requeue")
        }
      }).finally(() => {
        this.isBusy = false;
      });
    },
    selectAll(checked) {
      if (checked) {
        this.selectedItems = [
          ...this.items.map((item) => {
            return {
              ...item,
              selected: true,
            };
          }),
        ];
        this.items = [...this.selectedItems];
      } else {
        this.selectedItems = [];
        this.items = this.items.map((item) => {
          return {
            ...item,
            selected: false,
          };
        });
      }
    },
    select(item) {
      const { id } = item;
      if (!this.selectedItems.some((item) => item.id == id)) {
        this.selectedItems.push(item);
      } else {
        this.selectedItems = this.selectedItems.filter((item) => item.id != id);
      }
      this.isSelectedAll = this.selectedItems.length == this.items.length;
    },
    clearSelected() {
      this.allSelected = false;
      this.selectedItems = [];
    },
    info(item, button) {
      this.infoModal.title = item.id.toString();
      this.infoModal.content = JSONBIG({ storeAsString: true }).parse(item.content.trim());
      this.$root.$emit("bv::show::modal", this.infoModal.id, button);
    },
    pageSizeChange: function (size) {
      this.formData.perPage = size;
      this.fetchData();
    },
    onSearch: function () {
      this.fetchData();
    },
    requeue: function () {
      const _this = this;
      axios.post('/published/requeue', this.selectedItems.map((item) => item.id)).then(() => {
        this.selectedItems.map((item) => {
          _this.$bvToast.toast(this.$t("RequeueSuccess") + "   " + item.id, {
            title: "Tips",
            variant: "secondary",
            autoHideDelay: 1000,
            appendToast: true,
            solid: true
          });
        });
        _this.clear();
      });
    },
    clear() {
      this.items = this.items.map((item) => {
        return {
          ...item,
          selected: false,
        };
      });
      this.selectedItems = [];
      this.isSelectedAll = false;
    },
  },
};
</script>

<style scoped>
.pagination {
  flex: 1;
  justify-content: flex-end;
  align-items: center;
}

.capPagination::v-deep .page-link {
  color: #6c757d;
  box-shadow: none;
  border-color: #6c757d;
}

.capPagination::v-deep .page-link:hover {
  color: #fff;
  background-color: #6c757d;
  border-color: #6c757d;
}

.capPagination::v-deep .active .page-link {
  color: white;
  background-color: black;
}

.my-align-middle {
  vertical-align: middle;
}
</style>