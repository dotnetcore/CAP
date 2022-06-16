<template>
  <b-row>
    <b-col md="12">
      <h1 class="page-line mb-4">{{$t("Dashboard")}}</h1>
      <h3 class="mb-4">{{$t("24h History Graph")}}</h3>
      <v-chart class="chart" :option="option" />
    </b-col>
  </b-row>
</template>

<script>
import axios from 'axios';
import { use, graphic } from "echarts/core";
import { CanvasRenderer } from "echarts/renderers";
import { LineChart } from "echarts/charts";
import {
  GridComponent,
  DataZoomComponent,
  VisualMapComponent,
  TimelineComponent,
  CalendarComponent,
  TooltipComponent,
  LegendComponent,
} from "echarts/components";
import VChart, { THEME_KEY } from "vue-echarts";

use([
  CanvasRenderer,
  LineChart,
  GridComponent,
  DataZoomComponent,
  VisualMapComponent,
  TimelineComponent,
  CalendarComponent,
  TooltipComponent,
  LegendComponent
]);

export default {
  name: "Dashboard",
  components: {
    VChart
  },
  provide: {
    [THEME_KEY]: "light"
  },
  data() {
    return {
      renderData: {},
    }
  },
  computed: {
    option: function () {
      const { dayHour,
        publishSuccessed,
        publishFailed,
        subscribeSuccessed,
        subscribeFailed } = this.renderData;

      return {
        color: ['#80FFA5', '#00DDFF', '#37A2FF', '#FF0087'],
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'cross',
            label: {
              backgroundColor: '#6a7985'
            }
          }
        },
        legend: {
          data: [
            this.$t('Publish Succeeded'),
            this.$t('Publish Failed'),
            this.$t('Received Succeeded'),
            this.$t('Received Failed')]
        },
        grid: {
          left: '3%',
          right: '4%',
          bottom: '3%',
          containLabel: true
        },
        xAxis: [
          {
            type: 'category',
            inverse: true,
            axisLabel: {
              interval: 0,
              rotate: 40
            },
            boundaryGap: false,
            data: dayHour
          }
        ],
        yAxis: [
          {
            type: 'value'
          }
        ],
        series: [
          {
            name:  this.$t('Publish Succeeded'),
            type: 'line',
            stack: 'Number',
            smooth: true,
            lineStyle: {
              width: 0
            },
            showSymbol: false,
            areaStyle: {
              opacity: 0.8,
              color: new graphic.LinearGradient(0, 0, 0, 1, [{
                offset: 0,
                color: 'rgba(128, 255, 165)'
              }, {
                offset: 1,
                color: 'rgba(1, 191, 236)'
              }])
            },
            emphasis: {
              focus: 'series'
            },
            data: publishSuccessed
          },
          {
            name:  this.$t('Publish Failed'),
            type: 'line',
            stack: 'Number',
            smooth: true,
            lineStyle: {
              width: 0
            },
            showSymbol: false,
            areaStyle: {
              opacity: 0.8,
              color: new graphic.LinearGradient(0, 0, 0, 1, [{
                offset: 0,
                color: 'rgba(0, 221, 255)'
              }, {
                offset: 1,
                color: 'rgba(77, 119, 255)'
              }])
            },
            emphasis: {
              focus: 'series'
            },
            data: publishFailed
          },
          {
            name: this.$t('Received Succeeded'),
            type: 'line',
            stack: 'Number',
            smooth: true,
            lineStyle: {
              width: 0
            },
            showSymbol: false,
            areaStyle: {
              opacity: 0.8,
              color: new graphic.LinearGradient(0, 0, 0, 1, [{
                offset: 0,
                color: 'rgba(55, 162, 255)'
              }, {
                offset: 1,
                color: 'rgba(116, 21, 219)'
              }])
            },
            emphasis: {
              focus: 'series'
            },
            data: subscribeSuccessed
          },
          {
            name: this.$t('Received Failed'),
            type: 'line',
            stack: 'Number',
            smooth: true,
            lineStyle: {
              width: 0
            },
            showSymbol: false,
            areaStyle: {
              opacity: 0.8,
              color: new graphic.LinearGradient(0, 0, 0, 1, [{
                offset: 0,
                color: 'rgba(255, 0, 135)'
              }, {
                offset: 1,
                color: 'rgba(135, 0, 157)'
              }])
            },
            emphasis: {
              focus: 'series'
            },
            data: subscribeFailed
          }
        ]
      }
    }
  },

  mounted() {
    axios.get('/metrics').then(res => {
      this.renderData = res.data;
      console.log(this.renderData);
    });
  }
};
</script>

<style scoped>
.chart {
  height: 500px;
  width: 100%;
}
</style>