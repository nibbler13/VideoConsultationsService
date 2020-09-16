using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsNotificationService {
	class DbQueries {
		//new
		//992582623	Онлайн-консультация - гинекология
		//992582624	Онлайн-консультация - кардиология
		//992582625	Онлайн-консультация - неврология
		//992582626	Онлайн-консультация - терапия
		//992582628	Онлайн-консультация - оториноларингология
		//992092102	Санитарно просветительская работа
		//992582680 Педиатрия онлайн

		/* новая запись в расписании от сегодня и вперед */
		public const string sqlQueryGetNewSchedule =
			"SELECT " +
			"	Cl.phone1, " +
			"	Cl.phone2, " +
			"	Cl.phone3, " +
			"	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	EXTRACT(YEAR FROM Sh.workdate) AS DATA, " +
			"	Sh.bhour, " +
			"	Sh.bMin, " +
			"	Sh.SchedId " +
			"FROM Schedule Sh " +
			"	LEFT JOIN Clients Cl ON Cl.pcode = Sh.pcode " +
			"WHERE Sh.createdate >= current_date + DATEADD(minute, -10, current_time) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			"	AND Sh.OnlineType = 1";

		/* напоминание за 0-6 минут до начала пациентам*/
		public const string sqlQueryGetNewNotifications =
			"SELECT " +
			"	Cl.phone1, " +
			"	Cl.phone2, " +
			"	Cl.phone3, " +
			"	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	EXTRACT(YEAR FROM Sh.workdate) AS DATA, " +
			"	Sh.bhour, " +
			"	Sh.bMin, " +
			"	Sh.SchedId " +
			"FROM Schedule Sh " +
			"	LEFT JOIN Clients Cl ON Cl.pcode = Sh.pcode " +
			"WHERE Sh.workdate = 'TODAY' " +
			"	AND CAST('NOW' AS TIME) BETWEEN " +
			"   CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"	   IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) - 360 " +
			"	AND CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"		IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			// "	AND Ds.DepNum IN (992582623, 992582624, 992582625, 992582626, 992582628, 992092102, 992582680) " +
			"	AND Sh.OnlineType = 1";

		/* напоминание за 0-6 минут до начала докторам*/
		public const string sqlQueryGetNewNotificationsForDocs =
			"SELECT " +
			"	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	EXTRACT(YEAR FROM Sh.workdate) AS DATA, " +
			"	Sh.bhour, " +
			"	Sh.bMin, " +
			"	Sh.SchedId, " +
			"	D.PhoneInt " +
			"FROM Clients Cl " +
			"	JOIN Schedule Sh ON Cl.pcode = Sh.pcode " +
			"	JOIN Chairs Ch ON Ch.chid = Sh.chid " +
			"	JOIN Rooms R ON R.rid = Ch.rid " +
			"	JOIN DoctShedule Ds ON Ds.DCode = Sh.DCode AND Sh.WorkDate = Ds.WDate AND Sh.ChId = Ds.Chair " +
			"	JOIN Doctor D ON D.DCode = Sh.DCode " +
			"WHERE Sh.workdate = 'TODAY' " +
			"	AND CAST('NOW' AS TIME) BETWEEN " +
			"   CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"	   IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) - 360 " +
			"	AND CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"		IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			"  and " +
			"      (exists     (select null from dailyplandet where did = sh.planid and schid in (990054338,990054339,990054340)) or " +
			"       exists     (select null from scheduledet where schedid = sh.schedid and schid in (990054338,990054339,990054340)))";

		//проверка наличия напоминаний об оплате
		public const string sqlQueryGetPaymentNotifications =
			"select " +
			"f.shortname, " +
			"d.dname, " +
			"s.schedid, " +
			"s.workdate, " +
			"lpad(dateadd(minute, S.BHOUR * 60 + S.BMIN, cast('00:00' as time)), 5) WORKTIME, " +
			"c.fullname, " +
			"c.histnum, " +
			"c.bdate, " +
			"c.phone3, " +
			"c.phone2, " +
			"c.phone1, " +
			"s.createdate, " +
			"coalesce(p.SUMMARUB_DISC, 0) amount_payable, " +
			"coalesce(pay.amountrub, 0) paid_by_client, " +
			"iif(coalesce(c.WEBPAYTYPE, 0) <> 1 or coalesce(c.WEBACCESSTYPE, 0) <> 3, 'Нет', 'Да') online_account, " +
			"iif(p.did is null, 'Услуга запланирована','Предсчет') paytype, " +
			"(select list(j.jname|| ' дог.'|| A.AGNUM || ' ' || L.SHORTNAME, '<br>') from CLHISTNUM CL " +
			" join JPERSONS J on J.JID = CL.JID " +
			" join JPLISTS L on L.LID = CL.LSTID " +
			" join JPAGREEMENT A on A.AGRID = CL.AGRID " +
			" where CL.PCODE = S.PCODE and coalesce(CL.ISDELETED, 0) = 0 and " +
			" coalesce(CL.DATECANCEL, CL.FDATE) >= s.workdate and CL.BDATE <= s.workdate) CLHISTINFO, " +
			"dep.depname " +
			"from schedule s " +
			"join filials f on f.filid = s.filial " +
			"join doctor d on d.dcode = s.dcode " +
			"join clients c on c.pcode = s.pcode " +
			"join doctshedule ds on ds.schedident = s.schedident " +
			"join departments dep on dep.depnum = ds.depnum " +
			"left join dailyplan p on p.did = s.planid and p.PLANTYPE = 204 " +
			"left join ( " +
			"select pcode, planid, sum(iif(typeoper in (2,5, 102),-amountrub,amountrub)) amountrub from ( " +
			"  select PCode, iif(PayCode = 5, 2, 1) typeoper, planid, AmountRub from Incom " +
			"  where PayCode in (1,3) or (PayCode = 5 and IncRef not in (10,11)) " +
			"union all " +
			"  select PCode, 1 TypeOper, planid, AmountRub from LoseCredit " +
			"union all " +
			"  select PCode, 2 TypeOper, planid, AmountRub from JPPayments " +
			"  where OperType = 5 and (not IncRef  in (10, 11)) " +
			"union all " +
			"  select PCode, TypeOper, planid, AmountRub from ClAvans) group by 1,2) pay on pay.planid = s.planid and pay.pcode = s.pcode " +
			"where (exists (select null from dailyplandet where did = p.did and schid in (990054338,990054339,990054340)) or " +
			"  exists (select null from scheduledet where schedid = s.schedid and schid in (990054338,990054339,990054340)))";



		public const string NotificationByCreateDate = "  and s.createdate between dateadd(minute, -30, current_timestamp) and dateadd(minute, 30, current_timestamp)" +
			" and f.filid not in (9, 15)";
		public const string NotificationByWorktime = "  and s.workdate = current_date and datediff(minute, current_time, dateadd(minute, S.BHOUR * 60 + S.BMIN, cast('00:00' as time))) between 0 and 30" +
			" and f.filid not in (9, 15)";

		public const string NotificationByCreateDateOffset2H = "  and s.createdate between dateadd(minute, 90, current_timestamp) and dateadd(minute, 150, current_timestamp)" +
			" and f.filid in (9, 15)";
		public const string NotificationByWorktimeOffset2H = "  and s.workdate = current_date and datediff(minute, current_time, dateadd(minute, S.BHOUR * 60 + S.BMIN, cast('00:00' as time))) between 120 and 150" +
			" and f.filid in (9, 15)";

		public const string GetTelemedicineUserList = 
			"select " +
			"lower(d.ONLINEUSERID) as userid, " +
			"count(iif(dsx.isfree = 0 and dsx.end_time >= current_time and " +
			"    (exists (select null from dailyplandet where did = p.did and schid in (990054338,990054339,990054340,990054352)) or " +
			"    exists  (select null from scheduledet where schedid = dsx.schedid and schid in (990054338,990054339,990054340,990054352))), 1, null)) last_pac_day, " +
			"count(iif(dsx.isfree = 0 and dsx.end_time >= current_time - 1800 and dsx.beg_time <= current_time + 7200 and " +
			"    (exists (select null from dailyplandet where did = p.did and schid in (990054338,990054339,990054340,990054352)) or " +
			"    exists  (select null from scheduledet where schedid = dsx.schedid and schid in (990054338,990054339,990054340,990054352))), 1, null)) last_pac_2hours " +
			"from ( " +
			"    select " +
			"    ds.dcode, " +
			"    ds.filial, " +
			"    ds.depnum, " +
			"    ds.wdate, " +
			"    sx.SCHEDID, " +
			"    sx.isfree, " +
			"    cast('00:00' as time) + (sx.bhour * 3600 + sx.bmin * 60) beg_time, " +
			"    cast('00:00' as time) + (sx.fhour * 3600 + sx.fmin * 60) end_time, " +
			"    cast('00:00' as time) + (ds.beghour * 3600 + ds.begmin * 60) beg_doc_time, " +
			"    cast('00:00' as time) + (ds.endhour * 3600 + ds.endmin * 60) end_doc_time " +
			"    from doctshedule ds " +
			"    left join sched_intervals(ds.schedident, 990002986) sx on 1=1 " +
			"    where ds.wdate = current_date and " +
			"        ds.daytype = 1 " +
			"  ) dsx " +
			"join departments dep on dep.depnum = dsx.depnum " +
			"join schedule s on s.schedid = dsx.schedid " +
			"left join dailyplan p on p.did = s.planid and p.PLANTYPE = 204 " +
			"join doctor d on d.dcode = dsx.dcode and char_length(d.ONLINEUSERID) > 3 " +
			"group by 1";

		public const string GetRateSurveyNotifications = 
			"select  " +
			"	distinct mfs.schedid  " +
			"	,mfs.schedule_date  " +
			"	,mfs.pcode " +
			"	,mdc.fullname as patient " +
			"	,age_in_years(current_date, mdc.bdate)  " +
			"	,mdc.phone  " +
			"	,mdc.phone2  " +
			"	,mdc.phone3 " +
			"	,mfs.dcode " +
			"	,mdd.fullname as doctor  " +
			"	,ddl.link " +
			"	,mdd.profname  " +
			"	,mdd.doctpost  " +
			"	,mfs.target_fil_shortname  " +
			"	,mfs.department " +
			"	,mfo.docname " +
			"	,'' as accepted_to_send " +
			"from mis_fact_schedule mfs " +
			"join mis_dim_clients mdc on mdc.pcode = mfs.pcode " +
			"join mis_dim_doctors mdd on mdd.uid = mfs.dcode " +
			"left join temp.dim_doctors_links ddl on ddl.dcode = mfs.dcode " +
			"join mis_fact_orderdet mfo on mfo.treatcode = mfs.treatcode " +
			"join mis_dim_wschema mdw on mdw.schid = mfo.schid  " +
			"left join mis_fact_treat mft  " +
			"	on mft.dcode = mfs.dcode  " +
			"	and mft.pcode = mfs.pcode  " +
			"	and cast(mft.treatdate as date) < cast(mfs.schedule_date as date)  " +
			"where mfs.schedule_date >= current_date - 1 " +
			"	and ddl.link is not null " +
			"	and mfs.target_filid in (1, 5, 12, 3, 8, 10, 17) " + //15, К-Урал (+2 часа)
			"	and mfs.treatcode is not null " +
			"	and age_in_years(current_date, mdc.bdate) between 18 and 50  " +
			"	and mfo.is_nal = true " +
			"	and mfo.docname = 'Наличная категория приема' " +
			"	and mfo.schamount > 0 " +
			"	and (mdc.refusesms is null or mdc.refusesms = 0) " +
			"	and mft.treatcode is null " +
			"	and mfs.department not in ('ПРОЦЕДУРНЫЙ КАБИНЕТ', 'Взятие мазков (Заборники)') " +
			"	and lower(mdw.schname) not like '%анестез%'   " +
			"	and lower(mdw.schname) not like '%взятие%'   " +
			"	and lower(mdw.schname) not like '%онлайн%'   " +
			"	and lower(mdw.schname) not like '%диспансерный%'   " +
			"	and lower(mdw.schname) not like '%заключение%'   " +
			"	and lower(mdw.schname) not like '%звонок%'   " +
			"	and lower(mdw.schname) not like '%соскоб%'   " +
			"	and lower(mdw.schname) not like '%освидетельствование%'   " +
			"	and lower(mdw.schname) not like '%описание снимков%'   " +
			"	and lower(mdw.schname) not like '%педиатр%'   " +
			"	and lower(mdw.schname) not like '%детск%'   " +
			"	and lower(mdw.schname) not like '%псих%'   " +
			"	and lower(mdw.schname) not like '%ревматолог%'   " +
			"	and lower(mdw.schname) not like '%терапевт%'   " +
			"	and lower(mdw.schname) not like '%физиотерапевт%'   " +
			"	and lower(mdw.schname) not like '%программа%'   " +
			"	and lower(mdd.profname || mdd.doctpost) not like '%сестра%'  " +
			"	and lower(mdd.profname || mdd.doctpost) not like '%лаборант%'";

		public static readonly string sqlInsertSmsSendInfo =
			"INSERT INTO BZ_SMSLOG (" +
			"    SMSID, " +
			"    SENDDATE, " +
			"    HOSTNAME, " +
			"    USERNAME, " +
			"    RECIPIENTNAME, " +
			"    RECIPIENTPHONE, " +
			"    SMSTEXT, " +
			"    ISRIGHTNOW, " +
			"    ISDELAYED, " +
			"    DELAYEDTIME)" +
			"VALUES (" +
			"    @smsID, " +
			"    @sendDate, " +
			"    '" + Environment.MachineName + "', " +
			"    '" + Environment.UserName + "', " +
			"    @recipientName, " +
			"    @recipientPhone, " +
			"    @smsText, " +
			"    @isRightNow, " +
			"    @isDelayed, " +
			"    @delayedTime)";
	}
}
