using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
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
			"FROM Clients Cl " +
			"	JOIN Schedule Sh ON Cl.pcode = Sh.pcode " +
			"	JOIN Chairs Ch ON Ch.chid = Sh.chid " +
			"	JOIN Rooms R ON R.rid = Ch.rid " +
			"	JOIN DoctShedule Ds ON Ds.DCode = Sh.DCode AND Sh.WorkDate = Ds.WDate AND Sh.ChId = Ds.Chair " +
			"	JOIN Doctor D ON D.DCode = Sh.DCode " +
			"WHERE Sh.createdate >= current_date + DATEADD(minute, -10, current_time) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			// "	AND Ds.DepNum IN (992582623, 992582624, 992582625, 992582626, 992582628, 992092102, 992582680) " +
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
			"iif(p.did is null, 'Услуга запланирована','Предсчет') paytype " +
			"from schedule s " +
			"join filials f on f.filid = s.filial " +
			"join doctor d on d.dcode = s.dcode " +
			"join clients c on c.pcode = s.pcode " +
			"join doctshedule ds on ds.schedident = s.schedident " +
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

		public const string NotificationByCreateDate = "  and s.createdate between dateadd(minute, -30, current_timestamp) and dateadd(minute, 30, current_timestamp)";
		public const string NotificationByWorktime = "  and s.workdate = current_date and datediff(minute, current_time, dateadd(minute, S.BHOUR * 60 + S.BMIN, cast('00:00' as time))) between 0 and 30";
	}
}
