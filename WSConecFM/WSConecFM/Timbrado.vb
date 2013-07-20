'''***************************************************************************
'* Descripción: Ejemplo del uso de la clase WSConecFM de Facturacion Moderna para el
'* Timbrado y cancelacion de un comprobante generando un
'* archivo XML de un CFDI 3.2 y enviandolo a certificar.
'*
'* Nota: Esté ejemplo pretende ilustrar de manera general el proceso de sellado y
'* timbrado de un XML que cumpla con los requerimientos del SAT.
'* 
'* Facturación Moderna :  http://www.facturacionmoderna.com
'* @author Benito Arango <benito.arango@facturacionmoderna.com>
'* @package WSConecFM
'* @version 1.0
'*
'*****************************************************************************
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.IO
Imports System.Collections
Imports System.Xml
Imports System.ServiceModel
Imports System.Diagnostics

Public Class Timbrado
	Public Function Timbrar(layout As String, RequestTimbrarCFDI As WSConecFM.requestTimbrarCFDI, pathFile As String) As String()
		Dim respuesta As String() = {"0", "Timbrado del comprobante exitoso"}

		Try
			If File.Exists(layout) Then
				Dim objReader As New StreamReader(layout, Encoding.UTF8)
				layout = objReader.ReadToEnd()
				objReader.Close()
			End If

			' Codificar a base 64 el layout
			layout = Convert.ToBase64String(Encoding.UTF8.GetBytes(layout))

			' Agregar el XML codificado en base64 a la peticion SOAP
			RequestTimbrarCFDI.text2CFDI = layout

			'  Conexion con el WS de Facturacion Moderna
			Dim binding As New BasicHttpBinding()
			setBinding(binding)

			' Direccion del servicio SOAP de Prueba
			Dim endpoint As New EndpointAddress(RequestTimbrarCFDI.urltimbrado)

      ' Crear instancia al servisio SOAP de Timbrado
      Dim WSFModerna As New WSLayoutFacturacionModerna.Timbrado_ManagerPortClient(binding, endpoint)

			' Ejecutar servicio de Timbrado
			Dim objResponse As [Object] = WSFModerna.requestTimbrarCFDI(RequestTimbrarCFDI)

			If objResponse IsNot Nothing Then
				Dim xmlDoc As New XmlDocument()
				Dim xmlDeclaration As XmlDeclaration
				Dim xmlElementBody As XmlElement
				xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "uft-8", "no")
				xmlElementBody = xmlDoc.CreateElement("Container")
				xmlDoc.InsertBefore(xmlElementBody, xmlDoc.DocumentElement)
				Dim xmlParentNode As XmlElement
				xmlParentNode = xmlDoc.CreateElement("responseSoap")
				xmlDoc.DocumentElement.PrependChild(xmlParentNode)
				Dim nodosXmlResponse As XmlNode() = DirectCast(objResponse, XmlNode())
				For Each nodo As XmlNode In nodosXmlResponse
					If nodo.InnerText.Length >= 1 Then
						Dim xmlElemetResponse As XmlElement
						xmlElemetResponse = xmlDoc.CreateElement(nodo.Name.ToString())
						Dim xmlTextNode As XmlText
						xmlTextNode = xmlDoc.CreateTextNode(nodo.InnerText.ToString())
						xmlParentNode.AppendChild(xmlElemetResponse)
						xmlElemetResponse.AppendChild(xmlTextNode)
					End If
				Next

				'-->>Accedemos a los nodos de la respuesta del xml para obenter los valores retornados en base64 (xml, pdf, cbb, txt)
				Dim xmlElementCFDI As XmlElement
				'-->>Xml certificado (CFDI)
				xmlElementCFDI = DirectCast(xmlDoc.GetElementsByTagName("xml").Item(0), XmlElement)

				' Obtener UUID del Comprobante
				Dim uuid As String
				Dim cfdiXML As New XmlDocument()
				Dim binary As Byte() = Convert.FromBase64String(xmlElementCFDI.InnerText)
				Dim strOriginal As [String] = System.Text.Encoding.UTF8.GetString(binary)
				cfdiXML.LoadXml(strOriginal)
				Dim xmlElementTimbre As XmlElement
				xmlElementTimbre = DirectCast(cfdiXML.GetElementsByTagName("tfd:TimbreFiscalDigital").Item(0), XmlElement)
				uuid = xmlElementTimbre.GetAttribute("UUID")

				' Guardar el comprobante a un archivo
				'-->>Almacenamiento del Comprobante en XML
				Dim stream As New FileStream(pathFile & "\" & uuid & ".xml", FileMode.Create)
				Dim writerBinary As New BinaryWriter(stream)
				writerBinary.Write(Convert.FromBase64String(xmlElementCFDI.InnerText))
				writerBinary.Close()
				If Not File.Exists(pathFile & "\" & uuid & ".xml") Then
					respuesta(0) = "1"
					respuesta(1) = "Error: El comprobante en XML no se pudo escribir en " & pathFile & "\" & uuid & ".xml"
					Return respuesta
				End If

				'-->>Representación impresa del CFDI en formato PDF
				If RequestTimbrarCFDI.generarPDF Then
					Dim xmlElementPDF As XmlElement = DirectCast(xmlDoc.GetElementsByTagName("pdf").Item(0), XmlElement)
					'-->>Almacenamiento del Comprobante en PDF
					stream = New FileStream(pathFile & "\" & uuid & ".pdf", FileMode.Create)
					writerBinary = New BinaryWriter(stream)
					writerBinary.Write(Convert.FromBase64String(xmlElementPDF.InnerText))
					writerBinary.Close()
					If Not File.Exists(pathFile & "\" & uuid & ".pdf") Then
						respuesta(0) = "1"
						respuesta(1) = "Error: El comprobante en PDF no se pudo escribir en " & pathFile & "\" & uuid & ".pdf"
						Return respuesta
					End If
				End If

				'-->>Representación impresa del CFDI en formato TXT
				If RequestTimbrarCFDI.generarTXT Then
					Dim xmlElementTXT As XmlElement = DirectCast(xmlDoc.GetElementsByTagName("txt").Item(0), XmlElement)
					'-->>Almacenamiento del Comprobante en PDF
					stream = New FileStream(pathFile & "\" & uuid & ".txt", FileMode.Create)
					writerBinary = New BinaryWriter(stream)
					writerBinary.Write(Convert.FromBase64String(xmlElementTXT.InnerText))
					writerBinary.Close()
					If Not File.Exists(pathFile & "\" & uuid & ".txt") Then
						respuesta(0) = "1"
						respuesta(1) = "Error: El comprobante en TXT no se pudo escribir en " & pathFile & "\" & uuid & ".txt"
						Return respuesta
					End If
				End If

				'-->>Representación impresa del CFDI en formato PNG
				If RequestTimbrarCFDI.generarCBB Then
					Dim xmlElementCBB As XmlElement = DirectCast(xmlDoc.GetElementsByTagName("png").Item(0), XmlElement)
					'-->>Almacenamiento del Comprobante en PNG
					stream = New FileStream(pathFile & "\" & uuid & ".png", FileMode.Create)
					writerBinary = New BinaryWriter(stream)
					writerBinary.Write(Convert.FromBase64String(xmlElementCBB.InnerText))
					writerBinary.Close()
					If Not File.Exists(pathFile & "\" & uuid & ".png") Then
						respuesta(0) = "1"
						respuesta(1) = "Error: El comprobante en PNG no se pudo escribir en " & pathFile & "\" & uuid & ".png"
						Return respuesta
					End If
				End If

				respuesta(1) = "Exito: Su comprobante lo encuentra en " & pathFile & "\"
				Return respuesta
			Else
				respuesta(0) = "1"
				respuesta(1) = "El servicio de timbrado respondio con NULL"
				Return respuesta
			End If
		Catch e As Exception
			respuesta(0) = "1"
			respuesta(1) = "Error: " & e.Message
			Return respuesta
		End Try
	End Function

	Private Sub setBinding(binding As BasicHttpBinding)
		' Crear archivo app.config de forma manual
		binding.Name = "Timbrado_ManagerBinding"
		binding.CloseTimeout = System.TimeSpan.Parse("00:01:00")
		binding.OpenTimeout = System.TimeSpan.Parse("00:01:00")
		binding.ReceiveTimeout = System.TimeSpan.Parse("00:10:00")
		binding.SendTimeout = System.TimeSpan.Parse("00:01:00")
		binding.AllowCookies = False
		binding.BypassProxyOnLocal = False
		binding.HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard
		binding.MaxBufferSize = 65536
		binding.MaxBufferPoolSize = 524288
		binding.MaxReceivedMessageSize = 65536
		binding.MessageEncoding = System.ServiceModel.WSMessageEncoding.Text
		binding.TextEncoding = System.Text.Encoding.UTF8
		binding.TransferMode = System.ServiceModel.TransferMode.Buffered
		binding.UseDefaultWebProxy = True
		binding.ReaderQuotas.MaxDepth = 32
		binding.ReaderQuotas.MaxStringContentLength = 8192
		binding.ReaderQuotas.MaxArrayLength = 16384
		binding.ReaderQuotas.MaxBytesPerRead = 4096
		binding.ReaderQuotas.MaxNameTableCharCount = 16384
		binding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport
		binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None
		binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None
		binding.Security.Transport.Realm = ""
		binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName
		binding.Security.Message.AlgorithmSuite = System.ServiceModel.Security.SecurityAlgorithmSuite.[Default]
	End Sub
End Class

Public Class Cancelado
	Public Function Cancelar(RequestCancelarCFDI As WSConecFM.requestCancelarCFDI) As String()
		Dim respuesta As String() = {"0", "Exito: el UUID " & RequestCancelarCFDI.uuid & "ha sido cancelado"}

		Try
			'  Conexion con el WS de Facturacion Moderna
			Dim binding As New BasicHttpBinding()
			setBinding(binding)

			' Direccion del servicio SOAP de Prueba
			Dim endpoint As New EndpointAddress(RequestCancelarCFDI.urlcancelado)

			' Crear instancia al servisio SOAP de cancelado
      Dim WSFModerna As New WSLayoutFacturacionModerna.Timbrado_ManagerPortClient(binding, endpoint)

			' Ejecutar servicio de Cancelado
			Dim response As [Object] = WSFModerna.requestCancelarCFDI(RequestCancelarCFDI)
			If response IsNot Nothing Then
				Dim xmlDoc As New XmlDocument()
				Dim xmlDeclaration As XmlDeclaration
				Dim xmlElementBody As XmlElement
				xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "uft-8", "no")
				xmlElementBody = xmlDoc.CreateElement("Container")
				xmlDoc.InsertBefore(xmlElementBody, xmlDoc.DocumentElement)
				Dim xmlParentNode As XmlElement
				xmlParentNode = xmlDoc.CreateElement("responseSoap")
				xmlDoc.DocumentElement.PrependChild(xmlParentNode)
				Dim nodosXmlResponse As XmlNode() = DirectCast(response, XmlNode())
				For Each nodo As XmlNode In nodosXmlResponse
					If nodo.InnerText.Length >= 1 Then
						Dim xmlElemetResponse As XmlElement
						xmlElemetResponse = xmlDoc.CreateElement(nodo.Name.ToString())
						Dim xmlTextNode As XmlText
						xmlTextNode = xmlDoc.CreateTextNode(nodo.InnerText.ToString())
						xmlParentNode.AppendChild(xmlElemetResponse)
						xmlElemetResponse.AppendChild(xmlTextNode)
					End If
				Next
				Dim xmlElementMsg As XmlElement = DirectCast(xmlDoc.GetElementsByTagName("Message").Item(0), XmlElement)
				respuesta(1) = xmlElementMsg.InnerText
				Return respuesta
			Else
				respuesta(0) = "1"
				respuesta(1) = "El servicio de Cancelado respondio con NULL"
				Return respuesta
			End If
			'respuesta[1] = response;
			Return respuesta
		Catch e As Exception
			respuesta(0) = "1"
			respuesta(1) = "Error: " & e.Message
			Return respuesta
		End Try
	End Function

	Private Sub setBinding(binding As BasicHttpBinding)
		' Crear archivo app.config de forma manual
		binding.Name = "Timbrado_ManagerBinding"
		binding.CloseTimeout = System.TimeSpan.Parse("00:01:00")
		binding.OpenTimeout = System.TimeSpan.Parse("00:01:00")
		binding.ReceiveTimeout = System.TimeSpan.Parse("00:10:00")
		binding.SendTimeout = System.TimeSpan.Parse("00:01:00")
		binding.AllowCookies = False
		binding.BypassProxyOnLocal = False
		binding.HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard
		binding.MaxBufferSize = 65536
		binding.MaxBufferPoolSize = 524288
		binding.MaxReceivedMessageSize = 65536
		binding.MessageEncoding = System.ServiceModel.WSMessageEncoding.Text
		binding.TextEncoding = System.Text.Encoding.UTF8
		binding.TransferMode = System.ServiceModel.TransferMode.Buffered
		binding.UseDefaultWebProxy = True
		binding.ReaderQuotas.MaxDepth = 32
		binding.ReaderQuotas.MaxStringContentLength = 8192
		binding.ReaderQuotas.MaxArrayLength = 16384
		binding.ReaderQuotas.MaxBytesPerRead = 4096
		binding.ReaderQuotas.MaxNameTableCharCount = 16384
		binding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport
		binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None
		binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None
		binding.Security.Transport.Realm = ""
		binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName
		binding.Security.Message.AlgorithmSuite = System.ServiceModel.Security.SecurityAlgorithmSuite.[Default]
	End Sub
End Class
