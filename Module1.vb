'Elaborado por L.I. Samuel Muñoz Chavez
Imports System.IO
Imports System.Xml
Module Module1

    Sub Main()
        ' Argumentos esperados:  Ruta del layout, Nombre de los archivos de respuesta y la ruta de almacenamiento
        ' Ruta del layout: Ruta absoluta del archivo de texto contenedor del layout, para efectos de pruebas se incluyó dentro de los parametros de configuracion del proyecto un solo argumento "C:/factura_en_texto_ejemplo.txt"
        ' Nombre de archivos de respuesta: El nombre con el que se almacenara los archivos xml y pdf retornados de la llamada al Web Service de Facturación Moderna
        ' ruta de almacenamiento : Ruta de almacenamiento de los archivos de respuesta (xml y pdf)
        Dim arguments() As String
        arguments = System.Environment.GetCommandLineArgs()
        Dim WSLayoutFM As New WSLayoutFacturacionModerna.Timbrado_ManagerPortClient 'Objeto encargado de las peticiones al WS
        If (System.Environment.GetCommandLineArgs().Length >= 2) Then ' Como mínimo se requiere la ruta del layout a certificar
            Dim pathLayout As String
            pathLayout = arguments(1)
            If (Not File.Exists(pathLayout)) Then
                Console.WriteLine("El layout especificado no existe en el directorio de archivos: " + pathLayout)
                Console.ReadLine()
                Return
            End If
            Dim textLayout As TextReader = New StreamReader(pathLayout)
            Dim RequestTimbrarCFDI As New RequestTimbrarCFDI
            RequestTimbrarCFDI.requestTimbrarCFDI() ' Clase que contiene los parametros de conexion del Web service de timbrado
            RequestTimbrarCFDI.text2CFDI = Convert.ToBase64String(New System.Text.UTF8Encoding().GetBytes(textLayout.ReadToEnd())) 'Conversión del layout a Base64
            Dim objResponse As Object
            objResponse = WSLayoutFM.requestTimbrarCFDI(RequestTimbrarCFDI) 'Llamada al método de timbrado de CFDIS
            If (Not objResponse Is Nothing) Then
                ' Finalizada la llamada damos tratamiento al responseSoap como un xml para una mayor flexibilidad
                Dim xmlDoc As New XmlDocument
                Dim xmlDeclaration As XmlDeclaration
                Dim XmlElementBody As XmlElement
                xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "uft-8", "no")
                XmlElementBody = xmlDoc.CreateElement("Container")
                xmlDoc.InsertBefore(XmlElementBody, xmlDoc.DocumentElement)
                Dim xmlParentNode As XmlElement
                xmlParentNode = xmlDoc.CreateElement("responseSoap")
                xmlDoc.DocumentElement.PrependChild(xmlParentNode)
                For Each nodos As XmlNode In objResponse
                    If (nodos.InnerText.Length >= 1) Then
                        Dim xmlElementResponse As XmlElement
                        xmlElementResponse = xmlDoc.CreateElement(nodos.Name.ToString())
                        Dim xmlTexNode As XmlText
                        xmlTexNode = xmlDoc.CreateTextNode(nodos.InnerText.ToString())
                        xmlParentNode.AppendChild(xmlElementResponse)
                        xmlElementResponse.AppendChild(xmlTexNode)
                    End If
                Next nodos

                'Accedemos a los nodos de la respuesta del xml para obtener el cfdi y el pdf retornados
                Dim xmlElementCFDI As XmlElement
                xmlElementCFDI = xmlDoc.GetElementsByTagName("xml").Item(0) ' Xml certificado
                Dim xmlElementPDF As XmlElement
                xmlElementPDF = xmlDoc.GetElementsByTagName("pdf").Item(0) ' Representacion impresa del CFDI
                Try
                    Dim nameFileResponse, pathFile As String
                    nameFileResponse = ""
                    pathFile = "C:\" ' Por default creamos los archivos en C
                    If (System.Environment.GetCommandLineArgs().Length >= 3) Then ' El argumento con índice 2(Nombre del archivo de salida) se utiliza para nombrar los archivos contenedores de la respuesta del Web Service
                        nameFileResponse = arguments(2)
                    End If
                    If (System.Environment.GetCommandLineArgs().Length >= 4) Then 'En caso de contar con el parámetro con índice 3 (Ruta de almacenamiento) se almacenaran los dos archivos en dicha ruta
                        pathFile = arguments(3)
                    End If

                    If (nameFileResponse.ToString().Length = 0) Then ' En caso de que no se proporcione el nombre se toma el atributo UUID del CFDI para nombrar los archivos de respuesta
                        Dim cfdiXML As New XmlDocument
                        Dim binary As Byte() = Convert.FromBase64String(xmlElementCFDI.InnerText)
                        Dim strOriginal As String = System.Text.Encoding.UTF8.GetString(binary)
                        cfdiXML.LoadXml(strOriginal)
                        Dim xmlElementTimbre As XmlElement
                        xmlElementTimbre = cfdiXML.GetElementsByTagName("tfd:TimbreFiscalDigital").Item(0)
                        nameFileResponse = xmlElementTimbre.GetAttribute("UUID") ' UUID del CFDI
                    End If

                    Dim stream As New FileStream(pathFile + nameFileResponse + ".xml", FileMode.Create) ' Almacenamiento del CFDI en formato XML
                    Dim writeBinay As New BinaryWriter(stream)
                    writeBinay.Write(Convert.FromBase64String(xmlElementCFDI.InnerText))
                    writeBinay.Close()
                    stream = New FileStream(pathFile + nameFileResponse + ".pdf", FileMode.Create) ' Almacenamiento de la representación impresa en PDF
                    writeBinay = New BinaryWriter(stream)
                    writeBinay.Write(Convert.FromBase64String(xmlElementPDF.InnerText))
                    writeBinay.Close()
                    Console.WriteLine("Proceso de certificación finalizado exitosamente")
                Catch ex As Exception
                    MsgBox(ex.ToString)
                End Try
            End If
        Else
            Console.WriteLine("Proporcione los parámetros del timbrado (Ruta del layout, Ruta de almacenamiento y opcionalmente el nombre de los archivos de salida)")
        End If

        Dim requestCancelarCFDI As RequestCancelarCFDI = New RequestCancelarCFDI
        requestCancelarCFDI.requestCancelarCFDI()
        requestCancelarCFDI.CancelarCFDI.UUID = "453F8434-0CAB-4445-9027-A481F7B8B117" ' Indicamos el UUID del CFDI que se desea cancelar
        Dim responseSoapCancelacion As String
        responseSoapCancelacion = WSLayoutFM.requestCancelarCFDI(requestCancelarCFDI)
        Console.WriteLine(responseSoapCancelacion)
        Console.ReadKey()

    End Sub

End Module
