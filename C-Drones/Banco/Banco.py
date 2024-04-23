from flask import Flask, request, jsonify
import pyodbc

app = Flask(__name__)

# Conexión a la base de datos en el servidor GENESIS utilizando autenticación de Windows
conn = pyodbc.connect('DRIVER={SQL Server};SERVER=GENESIS;DATABASE=Banco;Trusted_Connection=yes')

@app.route('/validar_tarjeta', methods=['POST'])
def validar_tarjeta():
    try:
        data = request.json
        numero_tarjeta = data.get('NumeroTarjeta')
        fecha_expiracion = data.get('FechaExpiracion')
        cvv = data.get('CVV')
        monto = data.get('Monto')

        print("Datos de tarjeta recibidos:")
        print("Número de tarjeta:", numero_tarjeta)
        print("Fecha de expiración:", fecha_expiracion)
        print("CVV:", cvv)
        print("Monto:", monto)

        if None in [numero_tarjeta, fecha_expiracion, cvv, monto]:
            mensaje = 'Parámetros incompletos'
            print("Mensaje:", mensaje)
            return jsonify({'exito': False, 'mensaje': mensaje})

        # Consultar la base de datos para validar la tarjeta y verificar el saldo
        cursor = conn.cursor()
        cursor.execute("EXEC ValidarTarjeta @NumeroTarjeta = ?, @CVV = ?, @Monto = ?, @FechaExpiracion = ?, @Resultado = ?",
                        (numero_tarjeta, cvv, monto, fecha_expiracion, 0))
        resultado_validacion = cursor.fetchone()[0]

        # Verificar si la tarjeta fue validada correctamente
        if resultado_validacion != 1:
            return jsonify({'exito': False, 'mensaje': 'Tarjeta inválida'})

        # Consultar el saldo de la tarjeta para verificar si es suficiente
        cursor.execute("EXEC VerificarSaldoTarjeta @NumeroTarjeta = ?, @Monto = ?, @Resultado = ?",
                       (numero_tarjeta, monto, 0))
        saldo_suficiente = cursor.fetchone()[0]

        # Verificar si el saldo es suficiente para realizar la transacción
        if saldo_suficiente != 1:
            return jsonify({'exito': False, 'mensaje': 'Saldo insuficiente'})

        # Lógica para realizar la transacción con tarjeta
        mensaje = 'Transacción realizada exitosamente'
        print("Mensaje:", mensaje)
        return jsonify({'exito': True, 'mensaje': mensaje})

    except Exception as e:
        mensaje = 'Error al procesar la transacción: ' + str(e)
        print("Mensaje:", mensaje)
        return jsonify({'exito': False, 'mensaje': mensaje})


@app.route('/realizar_transaccion', methods=['POST'])
def realizar_transaccion():
    try:
        data = request.json
        numero_cuenta = data.get('NumeroCuenta')
        monto = data.get('Monto')

        print("Datos de usuario recibidos:")
        print("Número de cuenta:", numero_cuenta)
        print("Monto a transferir:", monto)

        if numero_cuenta is None or monto is None:
            mensaje = 'Parámetros incompletos'
            print("Mensaje:", mensaje)
            return jsonify({'exito': False, 'mensaje': mensaje})

        # Consultar la base de datos para verificar si la cuenta existe
        cursor = conn.cursor()
        cursor.execute("SELECT COUNT(*) FROM Cuentas WHERE NumeroCuenta = ?", (numero_cuenta,))
        cuenta_existe = cursor.fetchone()[0]

        if cuenta_existe:
            # Consultar el saldo de la cuenta
            cursor.execute("SELECT Saldo FROM Cuentas WHERE NumeroCuenta = ?", (numero_cuenta,))
            saldo_actual = cursor.fetchone()[0]

            if saldo_actual >= monto:
                # Lógica para realizar la transacción
                mensaje = 'Transacción realizada exitosamente'
                print("Mensaje:", mensaje)
                return jsonify({'exito': True, 'mensaje': mensaje})
            else:
                mensaje = 'Saldo insuficiente para realizar la transacción'
                print("Mensaje:", mensaje)
                return jsonify({'exito': False, 'mensaje': mensaje})
        else:
            mensaje = 'La cuenta no existe en la base de datos'
            print("Mensaje:", mensaje)
            return jsonify({'exito': False, 'mensaje': mensaje})

    except Exception as e:
        mensaje = 'Error al procesar la transacción: ' + str(e)
        print("Mensaje:", mensaje)
        return jsonify({'exito': False, 'mensaje': mensaje})


def obtener_cuentas():
    try:
        # Conexión a la base de datos
        conn = pyodbc.connect('DRIVER={SQL Server};SERVER=GENESIS;DATABASE=Banco;Trusted_Connection=yes')

        # Crear un cursor para ejecutar consultas SQL
        cursor = conn.cursor()

        # Consultar la base de datos para obtener todos los registros de la tabla "Cuentas"
        cursor.execute("SELECT * FROM Cuentas")

        # Recorrer los resultados y mostrarlos en la consola
        print("Registros en la tabla Cuentas:")
        for row in cursor:
            print(row)

    except Exception as e:
        print("Error:", e)

    finally:
        # Cerrar la conexión con la base de datos
        conn.close()


# Llamar al método para obtener y mostrar las cuentas
obtener_cuentas()

if __name__ == '__main__':
    print("El servicio se está ejecutando...")
    app.run(debug=True)
