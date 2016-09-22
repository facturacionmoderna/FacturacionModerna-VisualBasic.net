Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Comprobante
Imports WSConecFM
Imports System.IO
Imports System

' NOTAS
'     * *    Todas las peticiones a los metodos de los Dlls retornan un objeto de tipo Resultados
'     * *    que contiene los siguientes atributos:
'     * *    1.- code; Codigo de error
'     * *    2.- message; Mensaje de error, mensaje de exito o resultado
'     * *    3.- status; solo toma los valores de True o False
'     * *    4.- ncert; contiene el numero de certificado, solo en algunos casos
' 

Public Class Form1

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ' Especificación de rutas especificas
        Dim currentPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName
        Dim keyfile As String = currentPath + "\utilerias\certificados\20001000000200000192.key"
        Dim certfile As String = currentPath + "\utilerias\certificados\20001000000200000192.cer"
        Dim password As String = "12345678a"
        Dim xsltPath As String = currentPath + "\utilerias\xslt3_2\cadenaoriginal_3_2.xslt"
        Dim xmlfile As String = TextBox1.Text()
        Dim path As String = currentPath + "\resultados"
        Dim cert_b64 As String = ""
        Dim cert_No As String = ""
        Dim newXml As String = ""
        Dim cadenaO As String = ""
        Dim sello As String = ""

        If Not System.IO.File.Exists(xmlfile) Then
            MessageBox.Show("El archivo " + xmlfile + " No existe")
            Environment.Exit(-1)
        End If

        Cursor.Current = Cursors.WaitCursor
        Dim r_comprobante As New Comprobante.Utilidades()
        Dim r_wsconect As New WSConecFM.Resultados()

        '  OBTENER LA INFORMACION DEL CERTIFICADO
        '  Los parametros enviados son:
        '    1.- Ruta del certificado

        If (r_comprobante.getInfoCertificate(certfile)) Then
            cert_b64 = r_comprobante.getCertificate()
            cert_No = r_comprobante.getCertificateNumber()
        Else
            MessageBox.Show(r_comprobante.getMessage())
            Environment.Exit(-1)
        End If

        '  AGREGAR INFORMACION DEL CERTIFICADO AL XML ANTES DE GENERAR LA CADENA ORIGINA
        '   Los parametros enviados son:
        '     1.- Xml (Puede ser una cadena o una ruta)
        '     2.- Certificado codificado en base 64
        '     3.- Numero de certificado
        '   Retorna el XML Modificado

        newXml = r_comprobante.addDigitalStamp(xmlfile, cert_b64, cert_No)
        If (newXml.Equals("")) Then
            MessageBox.Show(r_comprobante.getMessage())
            Environment.Exit(-1)
        End If

        xmlfile = newXml


        '  GENERAR CADENA ORIGINAL
        '    Los paramteros enviado son:
        '      1.- xml (Puede ser una cadena o una ruta)
        '      2.- xslt (Ruta del archivo xslt, con el cual se construye la cadena original)
        '   Retorna la cadena original

        cadenaO = r_comprobante.createOriginalChain(xmlfile, xsltPath)
        If (cadenaO.Equals("")) Then
            MessageBox.Show(r_comprobante.getMessage())
            Environment.Exit(-1)
        End If

        '  GENERAR EL SELLO DEL COMPROBANTE
        '     * *    Los parametros enviado son:
        '     * *    1.- archivo de llave privada (.key)
        '     * *    2.- Contraseña del archivo de llave privada
        '     * *    3.- Cadena Original (Puede ser una cadena o una ruta)
        '     * Retorna el sello en r_comprobante.message

        sello = r_comprobante.createDigitalStamp(keyfile, password, cadenaO)
        If (sello.Equals("")) Then
            MessageBox.Show(r_comprobante.getMessage())
            Environment.Exit(-1)
        End If

        '  AGREGAR LA INFORMACION DEL SELLO AL XML
        '   * *    Los parametros enviados son:
        '   * *    1.- Xml (Puede ser una cadena o una ruta)
        '   * *    2.- Sello del comprobante
        '   Retorna el XML Modificado

        newXml = r_comprobante.addDigitalStamp(xmlfile, sello)
        If (newXml.Equals("")) Then
            MessageBox.Show(r_comprobante.getMessage())
            Environment.Exit(-1)
        End If

        '  CREAR LA CONFIGURACION DE CONEXION CON EL SERVICIO SOAP
        '             * *    Los parametros configurables son:
        '             * *    1.- string UserID; Nombre de usuario que se utiliza para la conexion con SOAP
        '             * *    2.- string UserPass; Contraseña del usuario para conectarse a SOAP
        '             * *    3.- string emisorRFC; RFC del contribuyente
        '             * *    4.- Boolean generarCBB; Indica si se desea generar el CBB
        '             * *    5.- Boolean generarTXT; Indica si se desea generar el TXT
        '             * *    6.- Boolean generarPDF; Indica si se desea generar el PDF
        '             * *    7.- string urlTimbrado; URL de la conexion con SOAP
        '             * La configuracion inicial es para el ambiente de pruebas
        '            

        Dim reqt As New requestTimbrarCFDI()
        reqt.generarPDF = True
        '
        '             * Si desea cambiar alguna configuracion realizarla solo realizar lo siguiente
        '             * reqt.generarPDF = true;  Por poner un ejemplo
        '            


        '  TIMBRAR XML
        '             * *    Los parametros enviados son:
        '             * *    1.- XML; (Acepta una ruta o una cadena)
        '             * *    2.- Objeto con las configuraciones de conexion con SOAP
        '             * Retorna un objeto con los siguientes valores codificado en base 64:
        '             * *    1.- xml en base 64
        '             * *    2.- pdf en base 64
        '             * *    3.- png en base 64
        '             * *    4.- txt en base 64
        '             * Los valores de retorno van a depender de la configuracion enviada a la función
        '             

        Dim timbra As New Timbrado()
        r_wsconect = timbra.Timbrar(newXml, reqt)
        If Not r_wsconect.status Then
            MessageBox.Show(r_wsconect.message)
            Environment.Exit(-1)
        End If

        '' Guardar archivo XML
        Dim byteXML As Byte() = System.Convert.FromBase64String(r_wsconect.xmlBase64)
        Dim swxml As New IO.FileStream(path & "\" & r_wsconect.uuid & ".xml", IO.FileMode.Create)
        swxml.Write(byteXML, 0, byteXML.Length)
        swxml.Close()

        '' Guardar archivo PDF
        If reqt.generarPDF Then
            Dim bytePDF As Byte() = System.Convert.FromBase64String(r_wsconect.pdfBase64)
            Dim swpdf As New IO.FileStream(path & "\" & r_wsconect.uuid & ".pdf", IO.FileMode.Create)
            swpdf.Write(bytePDF, 0, bytePDF.Length)
            swpdf.Close()
        End If

        '' Guardar archivo CBB
        If reqt.generarCBB Then
            Dim byteCBB As Byte() = System.Convert.FromBase64String(r_wsconect.cbbBase64)
            Dim swcbb As New IO.FileStream(path & "\" & r_wsconect.uuid & ".png", IO.FileMode.Create)
            swcbb.Write(byteCBB, 0, byteCBB.Length)
            swcbb.Close()
        End If

        MessageBox.Show("Comprobante guardado en " & path & "\")
        Cursor.Current = Cursors.[Default]
        Close()
    End Sub

  Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Cursor.Current = Cursors.WaitCursor
        Dim currentPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName
        Dim layoutFile As String = TextBox2.Text
        Dim path As String = currentPath + "\resultados"
        Dim r_wsconect As New WSConecFM.Resultados()

        If Not System.IO.File.Exists(layoutFile) Then
            MessageBox.Show("El archivo " + layoutFile + " No existe")
            Environment.Exit(-1)
        End If

        ' Crear instancia, para los para metros enviados a requestTimbradoCFDI
        Dim reqt As New requestTimbrarCFDI()
        reqt.generarPDF = True
        Dim timbra As New Timbrado()
        r_wsconect = timbra.Timbrar(layoutFile, reqt)

        If Not r_wsconect.status Then
            MessageBox.Show(r_wsconect.message)
            Environment.Exit(-1)
        End If

        '' Guardar archivo XML
        Dim byteXML As Byte() = System.Convert.FromBase64String(r_wsconect.xmlBase64)
        Dim swxml As New IO.FileStream(path & "\" & r_wsconect.uuid & ".xml", IO.FileMode.Create)
        swxml.Write(byteXML, 0, byteXML.Length)
        swxml.Close()

        '' Guardar archivo PDF
        If reqt.generarPDF Then
            Dim bytePDF As Byte() = System.Convert.FromBase64String(r_wsconect.pdfBase64)
            Dim swpdf As New IO.FileStream(path & "\" & r_wsconect.uuid & ".pdf", IO.FileMode.Create)
            swpdf.Write(bytePDF, 0, bytePDF.Length)
            swpdf.Close()
        End If

        '' Guardar archivo CBB
        If reqt.generarCBB Then
            Dim byteCBB As Byte() = System.Convert.FromBase64String(r_wsconect.cbbBase64)
            Dim swcbb As New IO.FileStream(path & "\" & r_wsconect.uuid & ".png", IO.FileMode.Create)
            swcbb.Write(byteCBB, 0, byteCBB.Length)
            swcbb.Close()
        End If

        MessageBox.Show("Comprobante guardado en " & path & "\")
        Cursor.Current = Cursors.[Default]
        Close()
    End Sub

  Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
    Cursor.Current = Cursors.WaitCursor
    Dim uuid As String = TextBox3.Text

    Dim r_wsconect As New WSConecFM.Resultados()

    Dim reqc As New requestCancelarCFDI()

    Dim conex As New Cancelado()
    r_wsconect = conex.Cancelar(reqc, uuid)

    MessageBox.Show(r_wsconect.message)
    Cursor.Current = Cursors.[Default]
    Close()
  End Sub
End Class
