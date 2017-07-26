SELECT
	Cl.phone1,
	Cl.phone2,
	Cl.phone3,
	Cl.fullname,
	Cl.pcode,
	Cl.histnum,
	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' ||
	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' ||
	EXTRACT(YEAR FROM Sh.workdate) AS DATA,
	Sh.bhour,
	Sh.bMin,
	Sh.SchedId

FROM Clients Cl
	JOIN Schedule Sh ON Cl.pcode = Sh.pcode
	JOIN Chairs Ch ON Ch.chid = Sh.chid
	JOIN Rooms R ON R.rid = Ch.rid
	JOIN DoctShedule Ds ON Ds.DCode = Sh.DCode AND Sh.WorkDate = Ds.WDate AND Sh.ChId = Ds.Chair
	JOIN Doctor D ON D.DCode = Sh.DCode

WHERE Sh.workdate = 'TODAY'
	AND CAST ('NOW' AS TIME) BETWEEN 
	CAST( IIF (Sh.bhour < 10,0 || Sh.bhour, Sh.bhour) || ':' || 
		IIF (Sh.bmin < 10,0 || Sh.bmin, Sh.bmin) AS TIME) - 360 
	AND CAST( IIF (Sh.bhour < 10,0 || Sh.bhour, Sh.bhour) || ':' || 
		IIF (Sh.bmin < 10,0 || Sh.bmin, Sh.bmin)  AS TIME)
	AND (Sh.emid IS NULL OR Sh.emid = 0)
	AND Ds.DepNum IN (992092102, 992092953, 992092954, 992092955, 992092956, 764)
	AND Sh.OnlineType = 1


	
	/* напоминание за 0-6 минут до начала */