<template>
  <b-container>
    <h2 class="page-line mb-4">{{ $t("Dashboard") }}</h2>
    <b-row>
      <b-col>
        <h3 class="mb-4">{{ $t("Realtime Metric Graph") }}</h3>
        <div id="realtimeGraph"></div>
        <p class="text-secondary">{{ $t("SubscriberInvokeMeanTime") }}</p>
      </b-col>
    </b-row>
    <b-row>
      <b-col>
        <h3 class="mt-4">{{ $t("24h History Graph") }}</h3>
        <div id="historyGraph"></div>
      </b-col>
    </b-row>
  </b-container>
</template>

<script>

import uPlot from '../assets/uPlot.esm.js';
import axios from 'axios';

export default {
  data() {
    return {
      timer: Number
    }
  },
  async mounted() {
    const params = new Proxy(new URLSearchParams(window.location.search), {
      get: (searchParams, prop) => searchParams.get(prop),
    });
    let accessToken = params.access_token;
    if (accessToken) {
      localStorage.setItem('token', accessToken)
    }

    const realtimeOpts = {
      width: 960,
      height: 400,
      cursor: {
        drag: {
          setScale: false,
        }
      },
      select: {
        show: false,
      },
      series: [
        {
          value: "{YYYY}/{MM}/{DD} {HH}:{mm}:{ss}"
        },
        {
          label: this.$t("Publish TPS"),
          show: true,
          scale: "s",
          width: 2,
          value: (u, v) => v == null ? "-" : v.toFixed(1) + "/s",
          stroke: "rgba(0,255,0,0.3)"
        },
        {
          label: this.$t("Consume TPS"),
          show: true,
          scale: "s",
          width: 2,
          value: (u, v) => v == null ? "-" : v.toFixed(1) + "/s",
          stroke: "rgba(255,0,0,0.3)",
        },
        {
          label: this.$t("Subscriber Invoke Time"),
          scale: "ms",
          width: 1,
          paths: u => null,
          points: {
            space: 0,
            stroke: "blue"
          },
          value: (u, v) => v == null ? "-" : v.toFixed(0) + "ms",
          stroke: "blue"
        }
      ],
      axes: [
        {
          space: 30,
          values: [
            [1, "{mm}:{ss}", "\n{YY}/{M}/{D}/ {HH}:{mm}", null, "\n{M}/{D} {HH}:{mm}", null, "\n{HH}:{mm}", null, 1],
          ]
        },
        {
          scale: "s",
          label: this.$t("Rate (TPS)"),
          //space: 20,
          ticks: {
            show: true,
            stroke: "#eee",
            width: 10,
            dash: [5],
            size: 5,
          },
          values: (self, ticks) => ticks.map(rawValue => rawValue + "/s"),
          incrs: [
            1, 5, 10, 30, 50, 100
          ]
        }, {
          side: 1,
          scale: "ms",
          //space: 20,
          label: this.$t("Elpsed Time (ms)"),
          size: 60,
          ticks: {
            show: true,
            stroke: "#eee",
            width: 10,
            dash: [5],
            size: 5,
          },
          incrs: [
            1, 10, 50, 100, 300, 500, 1000
          ],
          values: (u, vals, space) => vals.map(v => +v.toFixed(0) + "ms"),
          grid: { show: false },
        }
      ]
    };

    var metricInitData = [];
    async function reamtime() {
      await axios.get('/metrics-realtime').then(res => {
        metricInitData = res.data;
      });
    }
    await reamtime();
    let realtimeUplot = new uPlot(realtimeOpts, metricInitData, document.getElementById("realtimeGraph"));

    this.timer = setInterval(async function () {
      await reamtime();
      realtimeUplot.setData(metricInitData);
    }, 1000);

    // ----------------------------------- History ------------------------------------------------
    var historyInitData = [];

    await axios.get('/metrics-history').then(res => {
      historyInitData.push(res.data.dayHour);
      historyInitData.push(res.data.publishSuccessed);
      historyInitData.push(res.data.subscribeSuccessed);
      historyInitData.push(res.data.publishFailed);
      historyInitData.push(res.data.subscribeFailed);
    });

    var historyOpts = {
      width: 960,
      height: 400,
      cursor: {
        drag: {
          setScale: false,
        }
      },
      select: {
        show: false,
      },
      series: [
        { value: "{YYYY}/{MM}/{DD} {HH}:00" },
        {
          label: this.$t("Publish Succeeded"),
          fill: "rgba(0,255,0,0.3)",
        },
        {
          label: this.$t("Received Succeeded"),
          fill: "rgba(0,0,255,0.3)",
        },
        {
          label: this.$t("Publish Failed"),
          fill: "rgba(255,0,0,0.5)",
        },
        {
          label: this.$t("Received Failed"),
          fill: "rgba(255,255,0,0.5)",
        },
      ],
      axes: [
        {
          space: 30,
          values: [
            [60, "{HH}:00", "\n{YYYY}/{M}/{D}", null, "\n{M}/{D}", null, null, null, 1],
          ],
        },
        {
          label: this.$t("Aggregation Count"),
        }
      ]
    };

    new uPlot(historyOpts, historyInitData, document.getElementById("historyGraph"));
  },
  destroyed() {
    window.clearInterval(this.timer);
  }
}; 
</script>

<style>
@import "/src/assets/uPlot.min.css";

.chart {
  height: 500px;
  width: 100%;
}
</style>