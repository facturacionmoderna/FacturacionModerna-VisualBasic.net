Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

<Serializable> _
Public Class requestTimbrarCFDI
	Public text2CFDI As String
	Public UserID As String
	Public UserPass As String
	Public emisorRFC As String
	Public generarCBB As [Boolean]
	Public generarTXT As [Boolean]
	Public generarPDF As [Boolean]
	Public urltimbrado As String

	Public Sub New()
		' Configuración inicial para la conexion con el servios SOAP de Timbrado
		Me.UserID = "UsuarioPruebasWS"
		Me.UserPass = "b9ec2afa3361a59af4b4d102d3f704eabdf097d4"
		Me.emisorRFC = "ESI920427886"
		Me.generarCBB = False
		Me.generarPDF = False
		Me.generarTXT = False
		Me.urltimbrado = "https://t1demo.facturacionmoderna.com/timbrado/soap"
	End Sub
End Class
