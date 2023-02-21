import{n,B as o,a as r,b as c,c as l}from"./index.219b11d6.js";import{j as d}from"./index.33d1e354.js";const u={currentPage:1,perPage:10,name:"",content:""},m={components:{BIconInfoCircleFill:o,BIconArrowRepeat:r,BIconSearch:c},props:{status:{}},data(){return{subMens:[{variant:"secondary",text:"Succeeded",num:"publishedSucceeded",name:"/published/succeeded"},{variant:"danger",text:"Failed",name:"/published/failed",num:"publishedFailed"},{variant:"warning",text:"Delayed",name:"/published/delayed",num:"publishedDelayed"}],pageOptions:[10,20,50,100,500],selectedItems:[],isBusy:!1,tableValues:[],isSelectedAll:!1,formData:{...u},totals:0,items:[],infoModal:{id:"info-modal",title:"",content:"{}"},expiresTitle:this.$t("Expires"),requeueTitle:this.$t("Requeue")}},computed:{onMetric(){return this.$store.getters.getMetric},fields(){return[{key:"checkbox",label:""},{key:"id",label:this.$t("IdName")},{key:"retries",label:this.$t("Retries")},{key:"added",label:this.$t("Added"),formatter:a=>{if(a!=null)return new Date(a).format("yyyy-MM-dd hh:mm:ss")}},{key:"expiresAt",label:this.expiresTitle,formatter:a=>{if(a!=null)return new Date(a).format("yyyy-MM-dd hh:mm:ss")}}]}},mounted(){this.fetchData(),window.abc=this},watch:{status:function(){this.fetchData()},"formData.currentPage":function(){this.fetchData()}},methods:{fetchData(){this.isBusy=!0,l.get(`/published/${this.status}`,{params:this.formData}).then(a=>{this.items=a.data.items,this.totals=a.data.totals,this.status=="delayed"?(this.expiresTitle=this.$t("DelayedPublishTime"),this.requeueTitle=this.$t("PublishNow")):(this.expiresTitle=this.$t("Expires"),this.requeueTitle=this.$t("Requeue"))}).finally(()=>{this.isBusy=!1})},selectAll(a){a?(this.selectedItems=[...this.items.map(t=>({...t,selected:!0}))],this.items=[...this.selectedItems]):(this.selectedItems=[],this.items=this.items.map(t=>({...t,selected:!1})))},select(a){const{id:t}=a;this.selectedItems.some(e=>e.id==t)?this.selectedItems=this.selectedItems.filter(e=>e.id!=t):this.selectedItems.push(a),this.isSelectedAll=this.selectedItems.length==this.items.length},clearSelected(){this.allSelected=!1,this.selectedItems=[]},info(a,t){this.infoModal.title=a.id.toString(),this.infoModal.content=d.exports({storeAsString:!0}).parse(a.content.trim()),this.$root.$emit("bv::show::modal",this.infoModal.id,t)},pageSizeChange:function(a){this.formData.perPage=a,this.fetchData()},onSearch:function(){this.fetchData()},requeue:function(){const a=this;l.post("/published/requeue",this.selectedItems.map(t=>t.id)).then(()=>{a.clear(),a.$bvToast.toast(this.$t("RequeueSuccess"),{title:"Tips",autoHideDelay:500,appendToast:!1})})},clear(){this.items=this.items.map(a=>({...a,selected:!1})),this.selectedItems=[],this.isSelectedAll=!1}}};var f=function(){var t=this,e=t._self._c;return e("div",[e("b-row",[e("b-col",{attrs:{md:"3"}},[e("b-list-group",[e("b-tooltip",{attrs:{target:"tooltip",triggers:"hover",variant:"warning","custom-class":"my-tooltip-class",placement:"bottomright"}},[t._v(" "+t._s(t.$t("DelayedInfo"))+" ")]),t._l(t.subMens,function(s,i){return e("router-link",{key:s.text,staticClass:"list-group-item text-left list-group-item-secondary list-group-item-action",attrs:{"active-class":"active",to:s.name}},[t._v(" "+t._s(t.$t(s.text))+" "),i==t.subMens.length-1?e("b-icon-info-circle-fill",{attrs:{id:"tooltip"}}):t._e(),e("b-badge",{staticClass:"float-right",attrs:{variant:s.variant,pill:""}},[t._v(" "+t._s(t.onMetric[s.num])+" ")])],1)})],2)],1),e("b-col",{attrs:{md:"9"}},[e("h2",{staticClass:"page-line mb-3"},[t._v(t._s(t.$t("Published Message")))]),e("b-form",{staticClass:"d-flex"},[e("div",{staticClass:"col-sm-10"},[e("div",{staticClass:"form-row mb-2"},[e("label",{staticClass:"sr-only",attrs:{for:"form-input-name"}},[t._v(t._s(t.$t("Name")))]),e("b-form-input",{staticClass:"form-control",attrs:{id:"form-input-name",placeholder:t.$t("Name")},model:{value:t.formData.name,callback:function(s){t.$set(t.formData,"name",s)},expression:"formData.name"}})],1),e("div",{staticClass:"form-row"},[e("label",{staticClass:"sr-only",attrs:{for:"inline-form-input-content"}},[t._v(t._s(t.$t("Content")))]),e("b-form-input",{staticClass:"form-control",attrs:{id:"inline-form-input-content",placeholder:t.$t("Content")},model:{value:t.formData.content,callback:function(s){t.$set(t.formData,"content",s)},expression:"formData.content"}})],1)]),e("div",{staticClass:"align-self-end"},[e("b-button",{attrs:{variant:"dark"},on:{click:t.onSearch}},[e("b-icon-search"),t._v(" "+t._s(t.$t("Search"))+" ")],1)],1)])],1)],1),e("b-row",[e("b-col",{attrs:{md:"12"}},[e("b-btn-toolbar",{staticClass:"mt-4"},[e("b-button",{attrs:{size:"sm",variant:"dark",disabled:!t.selectedItems.length},on:{click:t.requeue}},[e("b-icon-arrow-repeat",{attrs:{"aria-hidden":"true"}}),t._v(" "+t._s(t.requeueTitle)+" ")],1),e("div",{staticClass:"pagination"},[e("span",{staticStyle:{"font-size":"14px"}},[t._v(t._s(t.$t("Page Size"))+":")]),e("b-button-group",{staticClass:"ml-2"},t._l(t.pageOptions,function(s){return e("b-button",{key:s,class:{active:t.formData.perPage==s},attrs:{variant:"outline-secondary",size:"sm"},on:{click:function(i){return t.pageSizeChange(s)}}},[t._v(t._s(s)+" ")])}),1)],1)],1),e("b-table",{staticClass:"mt-3",attrs:{id:"datatable",busy:t.isBusy,striped:"","thead-tr-class":"text-left","details-td-class":"align-middle","tbody-tr-class":"text-left",small:"",fields:t.fields,items:t.items,"select-mode":"range"},scopedSlots:t._u([{key:"table-busy",fn:function(){return[e("div",{staticClass:"text-center text-secondary my-2"},[e("b-spinner",{staticClass:"align-middle"}),e("strong",{staticClass:"ml-2"},[t._v(t._s(t.$t("Loading"))+"...")])],1)]},proxy:!0},{key:"head(checkbox)",fn:function(){return[e("b-form-checkbox",{on:{change:t.selectAll},model:{value:t.isSelectedAll,callback:function(s){t.isSelectedAll=s},expression:"isSelectedAll"}})]},proxy:!0},{key:"cell(checkbox)",fn:function(s){return[e("b-form-checkbox",{on:{change:function(i){return t.select(s.item)}},model:{value:s.item.selected,callback:function(i){t.$set(s.item,"selected",i)},expression:"data.item.selected"}})]}},{key:"cell(id)",fn:function(s){return[e("b-link",{on:{click:function(i){return t.info(s.item,i.target)}}},[t._v(" "+t._s(s.item.id)+" ")]),e("br"),t._v(" "+t._s(s.item.name)+" ")]}}])}),e("span",{staticClass:"float-left"},[t._v(" "+t._s(t.$t("Total"))+": "+t._s(t.totals)+" ")]),e("b-pagination",{staticClass:"capPagination",attrs:{"first-text":t.$t("First"),"prev-text":t.$t("Prev"),"next-text":t.$t("Next"),"last-text":t.$t("Last"),"total-rows":t.totals,"per-page":t.formData.perPage,"aria-controls":"datatable"},model:{value:t.formData.currentPage,callback:function(s){t.$set(t.formData,"currentPage",s)},expression:"formData.currentPage"}})],1)],1),e("b-modal",{attrs:{size:"lg",id:t.infoModal.id,title:"Id: "+t.infoModal.title,"ok-only":"","ok-variant":"secondary"}},[e("vue-json-pretty",{key:t.infoModal.id,attrs:{showSelectController:"",data:t.infoModal.content}})],1)],1)},h=[],p=n(m,f,h,!1,null,"caa36018",null,null);const _=p.exports;export{_ as default};
