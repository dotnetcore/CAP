<template>
  <b-row>
    <b-col md="12">
      <h1 class="page-line mb-4">{{ $t("Dashboard") }}</h1>

      <h3 class="mb-4">{{ $t("24h History Graph") }}</h3>
      <div id="realtimeGraph"></div>

    </b-col>
  </b-row>
</template>

<script>

import uPlot from '../assets/uPlot.esm.js';

export default {
  mounted() {


    function round2(val) {
      return Math.round(val * 100) / 100;
    }

    function round3(val) {
      return Math.round(val * 1000) / 1000;
    }

    function prepData(packed) {
      console.time('prep');

      // epoch,idl,recv,send,read,writ,used,free

      const numFields = packed[0];

      packed = packed.slice(numFields + 1);

      // 55,550 data points x 3 series = 166,650
      let data = [
        Array(packed.length / numFields),
        Array(packed.length / numFields),
        Array(packed.length / numFields),
        Array(packed.length / numFields),
      ];

      for (let i = 0, j = 0; i < packed.length; i += numFields, j++) {
        data[0][j] = packed[i] * 60;
        data[1][j] = round3(100 - packed[i + 1]);
        data[2][j] = round2(100 * packed[i + 5] / (packed[i + 5] + packed[i + 6]));
        data[3][j] = packed[i + 3];
      }

      console.timeEnd('prep');

      return data;
    }

    function makeChart(data) {
      console.time('chart');

      function sliceData(start, end) {
        let d = [];

        for (let i = 0; i < data.length; i++)
          d.push(data[i].slice(start, end));

        return d;
      }

      let interval = 100;

      const opts = {
        title: "Fixed length / sliding data slices",
        width: 800,
        height: 400,
        cursor: {
          drag: {
            setScale: false,
          }
        },
        select: {
          show: false,
        },
        series: [{},
        {
          label: "CPU",
          scale: "%",
          value: (u, v) => v == null ? "-" : v.toFixed(1) + "%",
          stroke: "red",
        },
        {
          label: "RAM",
          scale: "%",
          value: (u, v) => v == null ? "-" : v.toFixed(1) + "%",
          stroke: "blue",
        },
        {
          label: "TCP Out",
          scale: "mb",
          value: (u, v) => v == null ? "-" : v.toFixed(2) + " MB",
          stroke: "green",
        }
        ],
        axes: [{},
        {
          scale: '%',
          values: (u, vals, space) => vals.map(v => +v.toFixed(1) + "%"),
        },
        {
          side: 1,
          scale: 'mb',
          values: (u, vals, space) => vals.map(v => +v.toFixed(2) + " MB"),
          grid: {
            show: false
          },
        },
        ]
      };

      let start1 = 0;
      let len1 = 3000;

      let data1 = sliceData(start1, start1 + len1);
      let uplot1 = new uPlot(opts, data1, document.getElementById("realtimeGraph"));

      setInterval(function () {
        start1 += 10;
        let data1 = sliceData(start1, start1 + len1);
        uplot1.setData(data1);
      }, interval);

      //  wait.textContent = "Done!";
      console.timeEnd('chart');

    }


    fetch("data.json").then(r => r.json()).then(packed => {
      //  wait.textContent = "Rendering...";
      let data = prepData(packed);
      setTimeout(() => makeChart(data), 0);
    });

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