SELECT	top(100)IDStavkaDokumenta,
						SifraArtikal, 
						NazivArtikla, IDDokument
	FROM			tblStavkaDokumenta
						INNER JOIN tblArtikal
						ON tblStavkaDokumenta.IDArtikal= tblArtikal.IDArtikal
	WHERE			tblStavkaDokumenta.IDDokument= 286296
	ORDER	BY	SifraArtikal


	Select top(100) * from tblDokumentAvansPDV

	Select top(100) * from tblDokumentTroskovi where IDDokument=259602




	SELECT		tblDokumentTroskovi.IDDokumentTroskovi,
						NULL,
						NazivPartnera + ' (' + IDVrstaDokumenta + ': ' + BrojDokumenta + ')' as [LISTA ZAVISNIH TROSKOVA],
						Sum(Iznos) AS OSNOVICA,
							(SELECT Sum(IznosPDV)
							 FROM		tblDokumentTroskoviStavkaPDV
											INNER JOIN tblDokumentTroskoviStavka
											ON tblDokumentTroskoviStavkaPDV.IDDokumentTroskoviStavka = 
												 tblDokumentTroskoviStavka.IDDokumentTroskoviStavka
							 WHERE	tblDokumentTroskoviStavka.IDDokumentTroskovi = 
											tblDokumentTroskovi.IDDokumentTroskovi) AS PDV
  
	from			tblDokumentTroskovi 
						inner join tblPartner
						on tblDokumentTroskovi.IDPartner=tblPartner.IDPartner
						LEFT OUTER JOIN tblDokumentTroskoviStavka
						on tblDokumentTroskovi.IDDokumentTroskovi= tblDokumentTroskoviStavka.IDDokumentTroskovi			
  where			tblDokumentTroskovi.IDDokument=259602

	GROUP BY	tblDokumentTroskovi.IDDokumentTroskovi,
						NazivPartnera,
						IDVrstaDokumenta,
						BrojDokumenta
						
						
SELECT		tblDokumentTroskoviStavka.IDDokumentTroskovi,
						tblDokumentTroskoviStavka.IDDokumentTroskoviStavka,
						'  ' + UPPER(tblUlazniRacuniIzvedeni.Opis) AS [LISTA STAVKI TROŠKA],
						Iznos as OSNOVICA,
						sum(IznosPDV) as PDV

  from			tblDokumentTroskoviStavka 
						inner join tblUlazniRacuniIzvedeni
						on tblDokumentTroskoviStavka.IDUlazniRacuniIzvedeni= 
													tblUlazniRacuniIzvedeni.IDUlazniRacuniIzvedeni
						LEFT OUTER JOIN tblDokumentTroskoviStavkaPDV 
						ON tblDokumentTroskoviStavka.IDDokumentTroskoviStavka= 
													tblDokumentTroskoviStavkaPDV.IDDokumentTroskoviStavka
						INNER JOIN tblDokumentTroskovi
						ON tblDokumentTroskoviStavka.IDDokumentTroskovi= tblDokumentTroskovi.IDDokumentTroskovi

  where			tblDokumentTroskovi.IDDokument=259602

	GROUP	BY	tblDokumentTroskoviStavka.IDDokumentTroskovi,
						tblDokumentTroskoviStavka.IDDokumentTroskoviStavka,
						tblUlazniRacuniIzvedeni.Opis,
						Iznos
