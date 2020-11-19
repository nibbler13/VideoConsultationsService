select 
	distinct mfs.schedid 
	,mfs.schedule_date 
	,mfs.pcode
	,mdc.fullname as patient
	,age_in_years(current_date, mdc.bdate) 
	,mdc.phone 
	,mdc.phone2 
	,mdc.phone3
	,mfs.dcode
	,mdd.fullname as doctor 
	,ddl.link
	,mdd.profname 
	,mdd.doctpost 
	,mfs.target_fil_shortname 
	,mfs.department
	,mfo.docname
from mis_fact_schedule mfs
join mis_dim_clients mdc on mdc.pcode  = mfs.pcode 
join mis_dim_doctors mdd on mdd.uid = mfs.dcode
left join temp.dim_doctors_links ddl on ddl.dcode = mfs.dcode
join mis_fact_orderdet mfo on mfo.treatcode = mfs.treatcode 
join mis_dim_wschema mdw on mdw.schid = mfo.schid 
left join mis_fact_treat mft 
	on mft.dcode = mfs.dcode 
	and mft.pcode = mfs.pcode 
	and cast(mft.treatdate as date) < cast(mfs.schedule_date as date) 
where mfs.schedule_date >= current_date - 1
	and mfs.target_filid in (1, 5, 12, 3, 8, 10, 15, 17)
	and mfs.treatcode is not null
	and age_in_years(current_date, mdc.bdate) between 18 and 50 
	and mfo.is_nal = true
	and mfo.schamount > 0
	and mft.treatcode is null
	and mfs.department not in ('ПРОЦЕДУРНЫЙ КАБИНЕТ', 'Взятие мазков (Заборники)')
	and lower(mdw.schname) not like '%анестез%'  
	and lower(mdw.schname) not like '%взятие%'  
	and lower(mdw.schname) not like '%онлайн%'  
	and lower(mdw.schname) not like '%диспансерный%'  
	and lower(mdw.schname) not like '%заключение%'  
	and lower(mdw.schname) not like '%звонок%'  
	and lower(mdw.schname) not like '%соскоб%'  
	and lower(mdw.schname) not like '%освидетельствование%'  
	and lower(mdw.schname) not like '%описание снимков%'  
	and lower(mdw.schname) not like '%педиатр%'  
	and lower(mdw.schname) not like '%детск%'  
	and lower(mdw.schname) not like '%псих%'  
	and lower(mdw.schname) not like '%ревматолог%'  
	and lower(mdw.schname) not like '%терапевт%'  
	and lower(mdw.schname) not like '%физиотерапевт%'  
	and lower(mdw.schname) not like '%программа%'  
	and lower(mdd.profname || mdd.doctpost) not like '%сестра%' 
	and lower(mdd.profname || mdd.doctpost) not like '%лаборант%'