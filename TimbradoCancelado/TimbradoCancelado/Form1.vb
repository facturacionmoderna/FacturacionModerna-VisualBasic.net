Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
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
        Dim keyfile As String = "C:\FacturacionModernaVB\utilerias\certificados\20001000000200000192.key"
        Dim certfile As String = "C:\FacturacionModernaVB\utilerias\certificados\20001000000200000192.cer"
        Dim password As String = "12345678a"
        Dim version As String = "3.2"
        Dim xmlfile As String = TextBox1.Text()
        Dim path As String = "C:\FacturacionModernaVB\resultados"

        Cursor.Current = Cursors.WaitCursor
        Dim r_comprobante As New Comprobante.Resultados()
        Dim r_wsconect As New WSConecFM.Resultados()

        ' GENERAR CADENA ORIGINAL
        '             * *    Los paramteros enviado son:
        '             * *    1.- version del comprobante
        '             * *    2.- xml (Puede ser una cadena o una ruta)
        '             * Retorna la cadena original en r_comprobante.message
        '            

        Dim cadena As New Cadena()
        r_comprobante = cadena.GeneraCadena(version, xmlfile)
        If Not r_comprobante.status Then
            MessageBox.Show(r_comprobante.message)
            Close()
        End If
        Dim cadenaO As String = r_comprobante.message

        ' GENERAR EL SELLO DEL COMPROBANTE
        '             * *    Los parametros enviado son:
        '             * *    1.- archivo de llave privada (.key)
        '             * *    2.- Contraseña del archivo de llave privada
        '             * *    3.- Cadena Original (Puede ser una cadena o una ruta)
        '             * Retorna el sello en r_comprobante.message
        '            

        Dim sello As New Sello()
        r_comprobante = sello.GeneraSello(keyfile, password, cadenaO)
        If Not r_comprobante.status Then
            MessageBox.Show(r_comprobante.message)
            Close()
        End If
        Dim str_sello As String = r_comprobante.message

        '  OBTENER LA INFORMACION DEL CERTIFICADO
        '             * *    Los parametros enviados son:
        '             * *    1.- Ruta del certificado
        '             * Retorna el numero de certificado en r_comprobante.ncert
        '             * Retorna el certificado en base 64 en r_comprobante.message
        '            

        r_comprobante = sello.obtenCertificado(certfile)
        If Not r_comprobante.status Then
            MessageBox.Show(r_comprobante.message)
            Close()
        End If
        Dim cert_b64 As String = r_comprobante.message
        Dim cert_No As String = r_comprobante.ncert

        '  AGREGAR LA INFORMACION DEL SELLO AL XML
        '             * *    Los parametros enviados son:
        '             * *    1.- Xml (Puede ser una cadena o una ruta)
        '             * *    2.- Sello del comprobante
        '             * *    3.- Certificado codificado en base 64
        '             * *    4.- Numero de certificado
        '             * Retorna el XML en r_comprobante.message
        '            

        Dim objReader As New StreamReader(xmlfile, Encoding.UTF8)
        xmlfile = objReader.ReadToEnd()
        objReader.Close()
        r_comprobante = sello.agregaSello(xmlfile, str_sello, cert_b64, cert_No)
        If Not r_comprobante.status Then
            MessageBox.Show(r_comprobante.message)
            Application.[Exit]()
        End If
        xmlfile = r_comprobante.message

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
        r_wsconect = timbra.Timbrar(xmlfile, reqt)
        If Not r_wsconect.status Then
            MessageBox.Show(r_wsconect.message)
            Close()
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
    Dim layoutFile As String = TextBox2.Text
    Dim path As String = "C:\FacturacionModernaVB\resultados"
    Dim r_wsconect As New WSConecFM.Resultados()

    ' Crear instancia, para los para metros enviados a requestTimbradoCFDI
    Dim reqt As New requestTimbrarCFDI()
    reqt.generarPDF = True
    Dim timbra As New Timbrado()
    r_wsconect = timbra.Timbrar(layoutFile, reqt)

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
