Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
<Serializable()> Public Class RequestTimbrarCFDI
    Public text2CFDI As String
    Public UserID As String
    Public UserPass As String
    Public emisorRFC As String

    Public Sub requestTimbrarCFDI() 'Credenciales de acceso al Web Service        
        UserID = "UsuarioPruebasWS"
        UserPass = "b9ec2afa3361a59af4b4d102d3f704eabdf097d4"
        emisorRFC = "ESI920427886"
    End Sub

End Class
