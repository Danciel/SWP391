import {request} from "./client";
import {unsupported} from "./unsupported";
const r=(path,method="GET",body)=>request(path,{method,body});
const TYPE_CODES={"Online Account":"ACCOUNT","Crypto Wallet":"DIGITAL_WALLET","Bank Account":"BANK_ACCOUNT","Cloud Storage":"CLOUD_STORAGE","Document":"DOCUMENT","Other":"OTHER"};
const TYPE_NAMES=Object.fromEntries(Object.entries(TYPE_CODES).map(([name,code])=>[code,name]));
const mapAsset=a=>({...a,type:TYPE_NAMES[a.assetType]||a.assetType,accountType:a.provider||""});
const mapDms=d=>d?({...d,checkInDays:d.checkInIntervalDays,reminderDays:d.reminderBeforeDays,maxMissed:d.maxMissedCheckIns,missedCount:d.missedCheckIns,lastCheckIn:d.lastCheckInAt}):null;
const mapWill=w=>({...w,name:w.title||"",assets:(w.assets||[]).map(a=>({assetId:a.assetId,willAssetId:a.willAssetId,assetName:a.assetName,assetType:a.assetType,beneficiary:(w.assignments||[]).find(x=>x.willAssetId===a.willAssetId)?(w.beneficiaries||[]).find(b=>b.id===(w.assignments||[]).find(x=>x.willAssetId===a.willAssetId)?.beneficiaryDesignationId):null})),beneficiaries:w.beneficiaries||[],assignments:w.assignments||[],executor:w.executor?{id:w.executor.executorAccountId,name:w.executor.executorName,status:w.executor.status,}:null,dms:mapDms(w.dms),payment:null});
export const getExecutors=async()=> (await r("/executors")).map(e=>({...e,id:e.accountId,name:e.fullName,specialization:e.bio||"",rating:e.rating||0,experienceYears:e.experienceYears||0,completedCases:e.completedCases||0}));
export const getProfile=async()=>{const u=await r("/accounts/me");return {...u,phone:u.phoneNumber||"",identityNumber:null,faceVerified:null};};
export const sendPhoneOtp=unsupported("Phone OTP");
export const verifyPhoneOtp=unsupported("Phone OTP");
export const verifyIdentityNumber=unsupported("National ID verification");
export const verifyFace=unsupported("Face verification");
export const getAssets=async()=> (await r("/assets")).map(mapAsset);
export const getAssetSecret=async(id)=> (await r(`/assets/${id}/secret`)).secretData || {};
export const deleteAsset=async(id)=>r(`/asset-maintenance/${id}`,"DELETE");
const assetBody=(a,editing=false)=>{
 const assetType=TYPE_CODES[a.type]||a.type;
 const provider=assetType==="ACCOUNT" ? (a.accountType||"GMAIL").toUpperCase() : assetType;
 const secretData=assetType==="DOCUMENT"||assetType==="OTHER" ? {content:a.content||a.description||a.name} : {username:a.username||"",password:a.password||""};
 return {name:a.name,assetType,description:a.description||"",provider,secretData:editing && !a.username && !a.password ? null : secretData};
};
export const addAsset=async(userId,a)=>mapAsset(await r("/assets/validated","POST",assetBody(a)));
export const updateAsset=async(userId,id,a)=>mapAsset(await r(`/assets/${id}`,"PUT",assetBody(a,true)));
export const getWills=async()=> {
 const summaries=await r("/wills");
 // Summary API intentionally contains no counts; fetch real details rather than invent empty lists.
 return Promise.all(summaries.map(async w=>{
   try {return await getWill(null,w.id)}
   catch(err){return {...w,name:w.title||"",assets:null,beneficiaries:null,detailError:err.message}}
 }));
};
export const getWill=async(userId,id)=>mapWill(await r(`/wills/${id}`));
export async function saveWillDraft(userId,id,patch={}) {
  if(!id){if(!patch.name?.trim()) throw Error("Will name is required before creating a draft.");const created=await r("/wills","POST",{title:patch.name.trim(),description:patch.description||""});return getWill(userId,created.id);}
  const before=await getWill(userId,id);
  if(patch.name!==undefined||patch.description!==undefined){await r(`/wills/${id}`,"PUT",{title:patch.name??before.name,description:patch.description??before.description});}
  if(patch.assets){let assigned=new Set(before.assets.map(x=>x.assetId));for(const item of patch.assets){if(!assigned.has(item.assetId)){await r(`/wills/${id}/assets`,"POST",{assetId:item.assetId}); assigned.add(item.assetId);}} if(before.assets.some(a=>!patch.assets.some(x=>x.assetId===a.assetId))) throw Error("Removing a will asset is not implemented by backend yet.");}
  if(patch.executor?.id && patch.executor.id!==before.executor?.id) await r(`/wills/${id}/executor`,"POST",{executorAccountId:patch.executor.id});
  if(patch.dms) await r(`/wills/${id}/dms`,"PUT",{enabled:true,checkInIntervalDays:patch.dms.checkInDays,reminderBeforeDays:patch.dms.reminderDays,maxMissedCheckIns:patch.dms.maxMissed});
  return getWill(userId,id);
}
export const deleteWillDraft=async(userId,id,declined=false)=>r(declined ? `/will-maintenance/${id}/declined` : `/wills/${id}`,"DELETE");
export const submitWill=async(userId,id)=>r(`/wills/${id}/submit`,"POST");
export async function updateWillBeneficiary(userId,id,assetId,person){const w=await getWill(userId,id); const asset=w.assets.find(a=>a.assetId===assetId);if(!asset)throw Error("Asset is not attached to the will"); const beneficiary=w.beneficiaries.find(b=>person.accountId ? b.beneficiaryAccountId===person.accountId : b.email===person.email) || await r(`/wills/${id}/beneficiaries`,"POST",{beneficiaryAccountId:person.accountId||null,fullName:person.fullName,email:person.email,phone:person.phone,relationship:person.relationship});await r(`/wills/${id}/assignments`,"POST",{willAssetId:asset.willAssetId,beneficiaryDesignationId:beneficiary.id});return getWill(userId,id);}
export const updateWillOverview=(userId,id,patch)=>saveWillDraft(userId,id,patch);
export const changeWillExecutor=(userId,id,executor)=>saveWillDraft(userId,id,{executor});
export const updateWillDms=(userId,id,dms)=>saveWillDraft(userId,id,{dms});
export const checkInDms=async(userId,id)=>{await r(`/wills/${id}/dms/check-in`,"POST");return getWill(userId,id)};
export const cancelDeathHandover=unsupported("Cancel death handover");
export const requestImmediateHandover=unsupported("Immediate handover");

export async function searchBeneficiaryAccount(q){return r(`/accounts/search?${q.includes("@")?"email":"phone"}=${encodeURIComponent(q.trim())}`);}
export const getInheritedWills=async()=>[]; // Inheritance handover has no backend endpoint yet; do not fabricate cases.
export const getInheritedWill=unsupported("Inherited will details");
export const issueClaimKey=unsupported("Claim key");
export const confirmClaim=unsupported("Confirm claim");
export async function getOwnerDashboardData(){const [wills,assets]=await Promise.all([getWills(),getAssets()]);const main=wills.find(w=>w.status==="ACTIVE")||wills[0];const detail=main?await getWill(null,main.id):null;return {vaultStatus:detail?.status||"LOCKED",deadManSwitch:detail?.dms?{enabled:detail.dms.enabled,willId:detail.id,checkInCycleDays:detail.dms.checkInDays,lastCheckIn:detail.dms.lastCheckIn,nextCheckInDue:detail.dms.nextCheckInAt,missedCount:detail.dms.missedCount,maxMissed:detail.dms.maxMissed,caseOpen:false}:{enabled:false},assets:assets.map(a=>({...a,beneficiary:null})),executors:detail?.executor?[{id:detail.executor.id,name:detail.executor.name,status:detail.executor.status}]:[],beneficiaries:(detail?.beneficiaries||[]).map(b=>({id:b.id,name:b.fullName,assetsAssigned:(detail.assignments||[]).filter(a=>a.beneficiaryDesignationId===b.id).length})),recentActivity:[]};}
