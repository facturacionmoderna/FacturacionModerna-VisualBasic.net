Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

<Serializable> _
Public Class requestCancelarCFDI
	Public CancelarCFDI As CancelarCFDI
	Public UserID As String
	Public UserPass As String
	Public emisorRFC As String
	Public uuid As String
	Public urlcancelado As String

	Public Sub New()
		' Configuracion Inicial del a conexion cone l servios SOAP de cancelacion
		Me.CancelarCFDI = New CancelarCFDI()
		'-->>Credenciales de acceso al servidor de timbrado de Facturación moderna            
		Me.UserID = "UsuarioPruebasWS"
		Me.UserPass = "b9ec2afa3361a59af4b4d102d3f704eabdf097d4"
		Me.emisorRFC = "ESI920427886"
		Me.uuid = ""
		Me.urlcancelado = "https://t1demo.facturacionmoderna.com/timbrado/soap"
	End Sub
End Class

<Serializable> _
Public Class CancelarCFDI
	Public UUID As [String]
	'-->>Folio fiscal del CFDI que se cancelará (UUID)

	Public Sub New()
	End Sub
End Class
