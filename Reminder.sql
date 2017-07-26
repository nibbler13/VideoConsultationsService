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

WHERE (Sh.emid IS NULL OR Sh.emid = 0)
	AND Ds.DepNum IN (992092102, 992092953, 992092954, 992092955, 992092956, 764) 
	AND Sh.WorkDate >= 'TODAY' 
	AND CAST(Sh.CreateDate AS DATE) = 'TODAY' 
	AND CAST(Sh.CreateDate AS TIMESTAMP) >= CAST('NOW' AS TIMESTAMP) - 0.004
	AND Sh.OnlineType = 1

	
	
/* новая запись в расписании от сегодня и вперед */