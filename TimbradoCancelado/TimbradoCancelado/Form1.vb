Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Comprobante
Imports ConnectionWSFM
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
        Dim keyfile = currentPath + "\\utilerias\\certificados\\20001000000300022759.key"
        Dim certfile = currentPath + "\\utilerias\\certificados\\20001000000300022759.cer"
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
        '    Los parametros configurables son:
        '    * *    1.- Nombre de usuario que se utiliza para la conexion al Web Service
        '     * *    2.- Contraseña del usuario que se utiliza para la conexion al Web Service
        '     * *    3.- RFC Emisor
        '     * *    4.- Habilitar el retorno del CBB
        '     * *    5.- Habilitar el retorno del TXT
        '     * *    6.- Habilitar el retorno del PDF
        '     * *    7.- URL del Web Service (endpoint)
        '     * *    8.- Habilitar debug para guardar Request y Response (Si se habilita, se debe de especificar una ruta del archivo log)
        '     * * La configuracion inicial es para el ambiente de pruebas
        '    */
        Dim conX As new ConnectionFM()
        conX.setDebugMode(True)
        conX.setLogFilePath(currentPath + "\\logs\\log.txt")
        conX.setGenerarPdf(True)

        '/*  Timbrar Layout
        '     * *   Se envia el layout a timbrar, puede ser una xml o un txt, especificando la ruta del archivo
        '     * *   o un string conteniendo todo el layout
        '     */
        If conX.timbrarLayout(newXml) = True Then

            Dim byteXML As Byte() = System.Convert.FromBase64String(conX.getXmlB64())
            Dim swxml As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".xml"))), System.IO.FileMode.Create)
            swxml.Write(byteXML, 0, byteXML.Length)
            swxml.Close()

            If conX.getCbbB64() <> "" Then
                Dim byteCBB As Byte() = System.Convert.FromBase64String(conX.getCbbB64())
                Dim swcbb As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".png"))), System.IO.FileMode.Create)
                swcbb.Write(byteCBB, 0, byteCBB.Length)
                swcbb.Close()
            End If

            If conX.getPdfB64() <> "" Then
                Dim bytePDF As Byte() = System.Convert.FromBase64String(conX.getPdfB64())
                Dim swpdf As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".pdf"))), System.IO.FileMode.Create)
                swpdf.Write(bytePDF, 0, bytePDF.Length)
                swpdf.Close()
            End If

            If conX.getTxtB64() <> "" Then
                Dim byteTXT As Byte() = System.Convert.FromBase64String(conX.getTxtB64())
                Dim swtxt As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".txt"))), System.IO.FileMode.Create)
                swtxt.Write(byteTXT, 0, byteTXT.Length)
                swtxt.Close()
            End If

            MessageBox.Show("Comprobante guardado en " + path + "\\")
        Else
            MessageBox.Show("[" + conX.getErrorCode() + "] " + conX.getErrorMessage())
        End If

        Cursor.Current = Cursors.[Default]
        Close()
    End Sub

  Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Cursor.Current = Cursors.WaitCursor
        Dim currentPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName
        Dim layoutFile As String = TextBox2.Text
        Dim path As String = currentPath + "\resultados"

        If Not System.IO.File.Exists(layoutFile) Then
            MessageBox.Show("El archivo " + layoutFile + " No existe")
            Environment.Exit(-1)
        End If

        '  CREAR LA CONFIGURACION DE CONEXION CON EL SERVICIO SOAP
        '    Los parametros configurables son:
        '    * *    1.- Nombre de usuario que se utiliza para la conexion al Web Service
        '     * *    2.- Contraseña del usuario que se utiliza para la conexion al Web Service
        '     * *    3.- RFC Emisor
        '     * *    4.- Habilitar el retorno del CBB
        '     * *    5.- Habilitar el retorno del TXT
        '     * *    6.- Habilitar el retorno del PDF
        '     * *    7.- URL del Web Service (endpoint)
        '     * *    8.- Habilitar debug para guardar Request y Response (Si se habilita, se debe de especificar una ruta del archivo log)
        '     * * La configuracion inicial es para el ambiente de pruebas
        '    */
        Dim conX As New ConnectionFM()
        conX.setDebugMode(True)
        conX.setLogFilePath(currentPath + "\\logs\\log.txt")
        conX.setGenerarPdf(True)

        '/*  Timbrar Layout
        '     * *   Se envia el layout a timbrar, puede ser una xml o un txt, especificando la ruta del archivo
        '     * *   o un string conteniendo todo el layout
        '     */
        If conX.timbrarLayout(layoutFile) = True Then

            Dim byteXML As Byte() = System.Convert.FromBase64String(conX.getXmlB64())
            Dim swxml As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".xml"))), System.IO.FileMode.Create)
            swxml.Write(byteXML, 0, byteXML.Length)
            swxml.Close()

            If conX.getCbbB64() <> "" Then
                Dim byteCBB As Byte() = System.Convert.FromBase64String(conX.getCbbB64())
                Dim swcbb As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".png"))), System.IO.FileMode.Create)
                swcbb.Write(byteCBB, 0, byteCBB.Length)
                swcbb.Close()
            End If

            If conX.getPdfB64() <> "" Then
                Dim bytePDF As Byte() = System.Convert.FromBase64String(conX.getPdfB64())
                Dim swpdf As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".pdf"))), System.IO.FileMode.Create)
                swpdf.Write(bytePDF, 0, bytePDF.Length)
                swpdf.Close()
            End If

            If conX.getTxtB64() <> "" Then
                Dim byteTXT As Byte() = System.Convert.FromBase64String(conX.getTxtB64())
                Dim swtxt As New System.IO.FileStream((path + ("\\" + (conX.getUuid() + ".txt"))), System.IO.FileMode.Create)
                swtxt.Write(byteTXT, 0, byteTXT.Length)
                swtxt.Close()
            End If

            MessageBox.Show("Comprobante guardado en " + path + "\\")
        Else
            MessageBox.Show("[" + conX.getErrorCode() + "] " + conX.getErrorMessage())
        End If

        Cursor.Current = Cursors.[Default]
        Close()
    End Sub

  Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Cursor.Current = Cursors.WaitCursor
        Dim uuid As String = TextBox3.Text

        Dim conX As New ConnectionFM()
        If conX.cancelarCfdi(uuid) = True Then
            MessageBox.Show("[" + conX.getSuccessCode() + "] " + conX.getSuccessMessage())
        Else
            MessageBox.Show("[" + conX.getErrorCode() + "] " + conX.getErrorMessage())
        End If

        Cursor.Current = Cursors.[Default]
        Close()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        ' Show the dialog and get result.
        Dim result As DialogResult = OpenFileDialog1.ShowDialog()
        If result = DialogResult.OK Then
            ' Test result.
            TextBox1.Text = OpenFileDialog1.FileName
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        ' Show the dialog and get result.
        Dim result As DialogResult = OpenFileDialog2.ShowDialog()
        If result = DialogResult.OK Then
            ' Test result.
            TextBox2.Text = OpenFileDialog2.FileName
        End If
    End Sub
End Class
