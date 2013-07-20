<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
    Me.Button1 = New System.Windows.Forms.Button()
    Me.Button2 = New System.Windows.Forms.Button()
    Me.Button3 = New System.Windows.Forms.Button()
    Me.TextBox1 = New System.Windows.Forms.TextBox()
    Me.TextBox2 = New System.Windows.Forms.TextBox()
    Me.TextBox3 = New System.Windows.Forms.TextBox()
    Me.SuspendLayout()
    '
    'Button1
    '
    Me.Button1.Location = New System.Drawing.Point(526, 37)
    Me.Button1.Name = "Button1"
    Me.Button1.Size = New System.Drawing.Size(120, 31)
    Me.Button1.TabIndex = 0
    Me.Button1.Text = "Timbrar XML"
    Me.Button1.UseVisualStyleBackColor = True
    '
    'Button2
    '
    Me.Button2.Location = New System.Drawing.Point(526, 83)
    Me.Button2.Name = "Button2"
    Me.Button2.Size = New System.Drawing.Size(120, 31)
    Me.Button2.TabIndex = 1
    Me.Button2.Text = "Timbrar Layout"
    Me.Button2.UseVisualStyleBackColor = True
    '
    'Button3
    '
    Me.Button3.Location = New System.Drawing.Point(526, 130)
    Me.Button3.Name = "Button3"
    Me.Button3.Size = New System.Drawing.Size(120, 32)
    Me.Button3.TabIndex = 2
    Me.Button3.Text = "Cancelar UUID"
    Me.Button3.UseVisualStyleBackColor = True
    '
    'TextBox1
    '
    Me.TextBox1.Location = New System.Drawing.Point(34, 43)
    Me.TextBox1.Name = "TextBox1"
    Me.TextBox1.Size = New System.Drawing.Size(486, 20)
    Me.TextBox1.TabIndex = 3
    Me.TextBox1.Text = "C:\FacturacionModernaVB\ejemplos\ejemploTimbradoXML.xml"
    '
    'TextBox2
    '
    Me.TextBox2.Location = New System.Drawing.Point(34, 89)
    Me.TextBox2.Name = "TextBox2"
    Me.TextBox2.Size = New System.Drawing.Size(486, 20)
    Me.TextBox2.TabIndex = 4
    Me.TextBox2.Text = "C:\FacturacionModernaVB\ejemplos\ejemploTimbradoLayout.ini"
    '
    'TextBox3
    '
    Me.TextBox3.Location = New System.Drawing.Point(34, 137)
    Me.TextBox3.Name = "TextBox3"
    Me.TextBox3.Size = New System.Drawing.Size(486, 20)
    Me.TextBox3.TabIndex = 5
    Me.TextBox3.Text = "AF7D5463-C8D5-4395-9D5D-95356A2F36E9"
    '
    'Form1
    '
    Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
    Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
    Me.ClientSize = New System.Drawing.Size(668, 209)
    Me.Controls.Add(Me.TextBox3)
    Me.Controls.Add(Me.TextBox2)
    Me.Controls.Add(Me.TextBox1)
    Me.Controls.Add(Me.Button3)
    Me.Controls.Add(Me.Button2)
    Me.Controls.Add(Me.Button1)
    Me.Name = "Form1"
    Me.Text = "Ejemplos de Timbrado y Cancelacion"
    Me.ResumeLayout(False)
    Me.PerformLayout()

  End Sub
  Friend WithEvents Button1 As System.Windows.Forms.Button
  Friend WithEvents Button2 As System.Windows.Forms.Button
  Friend WithEvents Button3 As System.Windows.Forms.Button
  Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
  Friend WithEvents TextBox2 As System.Windows.Forms.TextBox
  Friend WithEvents TextBox3 As System.Windows.Forms.TextBox

End Class
