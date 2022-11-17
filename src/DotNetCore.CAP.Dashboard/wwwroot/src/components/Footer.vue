<template>
  <b-navbar fixed="bottom" toggleable="lg" type="light" variant="secondary">
    <b-container class="d-block">
      <b-row class="align-items-start">
        <b-col> {{ meta.cap?.name }} : {{ meta.cap?.version.substring(0, 5) }}</b-col>
        <b-col> Storage : {{ meta.storage?.name }} </b-col>
        <b-col> Broker: {{ meta.broker?.name }}</b-col>
      </b-row>
    </b-container>
  </b-navbar>

</template>
<script>
import axios from 'axios';
export default {
  data() {
    return {
      meta: {}
    }
  },
  async mounted() {
   await axios.get('/meta').then(res => {
      this.meta = res.data;
    });
  }
}
</script>
<style scoped>
.d-block {
  color: rgba(255, 255, 255, 0.6);
}
</style>