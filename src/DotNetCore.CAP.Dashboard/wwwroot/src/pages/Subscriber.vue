<template>
  <b-row>
    <h2 page-line mb-4>{{ $t("Subscriber") }}</h2>

    <b-table-simple caption-top bordered small responsive>
      <caption>{{ $t("SubscriberDescription") }}</caption>

      <b-thead head-variant="light">
        <b-tr>
          <b-th width="30%" class="text-left">{{ $t("Group") }}</b-th>
          <b-th class="text-left">{{ $t("Name") }}</b-th>
          <b-th class="text-left">{{ $t("Method") }}</b-th>
        </b-tr>
      </b-thead>
      <b-tbody>
        <template v-for="subscriber in subscribers">
          <b-tr :key="subscriber.group + index" v-for="(column, index) in subscriber.values">
            <b-td class="text-left align-middle" v-if="index == 0" :rowspan="subscriber.childCount">
              {{ subscriber.group }}
            </b-td>
            <b-td class="text-left align-middle">
              {{ column.topic }}
            </b-td>
            <b-td>
              <div class="snippet-code text-left align-middle">
                <code>
                   <pre><span class="type">{{ column.implName }}</span>:<br><span v-html="column.methodEscaped">{{ column.methodEscaped }}</span></pre>
                 </code>
              </div>
            </b-td>
          </b-tr>
        </template>
      </b-tbody>
    </b-table-simple>
  </b-row>
</template>
<script>
import axios from 'axios';

export default {
  data() {
    return {
      subscribers: {}
    }
  },
  mounted() {
    axios.get('/subscriber').then(response => {
      this.subscribers = response.data;
    });
  }
}
</script>
<style>
.snippet-code pre {
  margin: 0;
}

.snippet-code pre .comment {
  color: rgb(0, 128, 0);
}

.snippet-code pre .keyword {
  color: rgb(0, 0, 255);
}

.snippet-code pre .string {
  color: rgb(163, 21, 21);
}

.snippet-code pre .type {
  color: rgb(43, 145, 175);
}
</style>