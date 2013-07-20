Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Xml
Imports System.Xml.Xsl
Imports System.IO
Imports System.Diagnostics
Imports System.Security.Cryptography
Imports Comprobante.JavaScience

Public Class Cadena
  Public Function GeneraCadena(xsltFile As String, xmlFile As String, cadenaPath As String) As String()
    Dim respuesta As String() = {"0", "Generacion de Cadena Original con Exito"}
    Try
      Dim xsltDoc As New XslCompiledTransform(True)
      xsltDoc.Load(xsltFile)
      xsltDoc.Transform(xmlFile, cadenaPath)
      If File.Exists(cadenaPath) Then
        Return respuesta
      Else
        respuesta(0) = "1"
        respuesta(1) = "Error: No se creo la cadena original, Vuelva a intentarlo"
        Return respuesta
      End If
    Catch e As Exception
      respuesta(0) = "1"
      respuesta(1) = "Error: " & e.Message
      Return respuesta
    End Try
  End Function
End Class

Public Class Sello
  Public Function GeneraSello(keyfile As String, password As String, originalchain As String) As String()
    Dim respuesta As String() = {"0", "Generacion de Sello con Exito"}
    Try
      Dim strSello As String = ""
      Dim strPathLlave As String = keyfile
      Dim strLlavePwd As String = password
      If File.Exists(originalchain) Then
        Dim objReader As New StreamReader(originalchain, Encoding.UTF8)
        originalchain = objReader.ReadToEnd()
        objReader.Close()
      Else
        respuesta(0) = "1"
        respuesta(1) = "Error: No se encuentra el archivo con la cadena original"
        Return respuesta
      End If
      Dim strCadenaOriginal As String = originalchain
      Dim passwordSeguro As New System.Security.SecureString()
      passwordSeguro.Clear()
      For Each c As Char In strLlavePwd.ToCharArray()
        passwordSeguro.AppendChar(c)
      Next
      Dim llavePrivadaBytes As Byte() = System.IO.File.ReadAllBytes(strPathLlave)
      Dim rsa As RSACryptoServiceProvider = opensslkey.DecodeEncryptedPrivateKeyInfo(llavePrivadaBytes, passwordSeguro)
      Dim hasher As New SHA1CryptoServiceProvider()
      Dim bytesFirmados As Byte() = rsa.SignData(System.Text.Encoding.UTF8.GetBytes(originalchain), hasher)
      strSello = Convert.ToBase64String(bytesFirmados)
      If strSello = "" Then
        respuesta(0) = "1"
        respuesta(1) = "Error: El sello esta vacio"
        Return respuesta
      End If
      respuesta(1) = strSello
      Return respuesta
    Catch e As Exception
      respuesta(0) = "1"
      respuesta(1) = "Error: " & e.Message
      Return respuesta
    End Try
  End Function

  Public Function obtenCertificado(certificado As String) As String()
    Dim respuesta As String() = {"0", "Certificado Extraido con Exito"}
    Try
      Dim opensslPath As String = "OpenSSl.exe"
      Dim process1 As New Process()
      process1.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
      process1.StartInfo.FileName = opensslPath
      process1.StartInfo.Arguments = "enc -A -base64 -in " & certificado
      process1.StartInfo.UseShellExecute = False
      process1.StartInfo.ErrorDialog = False
      process1.StartInfo.RedirectStandardOutput = True
      process1.Start()
      Dim str_cert As [String] = process1.StandardOutput.ReadToEnd()
      respuesta(1) = str_cert
      Return respuesta
    Catch e As Exception
      respuesta(0) = "1"
      respuesta(1) = "Error: " & e.Message
      Return respuesta
    End Try

  End Function

  Public Function agregaSello(xmlfile As String, sello As String, certificado As String, numCertificado As String) As String()
    Dim respuesta As String() = {"0", "El sello fue agregado la comprobante con exito"}
    Try
      Dim xmlDoc As New XmlDocument()
      xmlDoc.Load(xmlfile)
      Dim xmlNodoLista As XmlNodeList = xmlDoc.GetElementsByTagName("cfdi:Comprobante")

      For Each nodo As XmlNode In xmlNodoLista
        nodo.SelectSingleNode("@sello").InnerText = sello
        nodo.SelectSingleNode("@certificado").InnerText = certificado
        nodo.SelectSingleNode("@noCertificado").InnerText = numCertificado
      Next
      xmlDoc.Save(xmlfile)
      Return respuesta
    Catch e As Exception
      respuesta(0) = "1"
      respuesta(1) = "Error: " & e.Message
      Return respuesta
    End Try
  End Function
End Class
