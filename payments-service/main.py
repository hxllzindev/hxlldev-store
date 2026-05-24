import json
import os
import random
import threading
from datetime import datetime
from fastapi import FastAPI
from confluent_kafka import Producer, Consumer
from database import SessionLocal, PaymentEvent

app = FastAPI()

# Configurar produtor Kafka
KAFKA_BOOTSTRAP_SERVERS = os.getenv("KAFKA_BOOTSTRAP_SERVERS", "localhost:9092")
producer = Producer({'bootstrap.servers': KAFKA_BOOTSTRAP_SERVERS})

def save_event(order_id: str, event_type: str, data: dict):
    """Salva evento de pagamento no PostgreSQL"""
    db = SessionLocal()
    try:
        evt = PaymentEvent(
            order_id=order_id,
            event_type=event_type,
            data=json.dumps(data),
            timestamp=datetime.utcnow()
        )
        db.add(evt)
        db.commit()
        print(f"💾 Evento salvo: {event_type} - Pedido {order_id}")
    except Exception as e:
        print(f"Erro ao salvar evento: {e}")
        db.rollback()
    finally:
        db.close()

def consume_payment_commands():
    """Thread que consome comandos de pagamento do Kafka"""
    consumer = Consumer({
        'bootstrap.servers': KAFKA_BOOTSTRAP_SERVERS,
        'group.id': 'payments-group',
        'auto.offset.reset': 'earliest'
    })
    consumer.subscribe(['payment-commands'])
    print("💳 Serviço de pagamentos aguardando comandos...")

    while True:
        msg = consumer.poll(1.0)
        if msg is None:
            continue
        if msg.error():
            print(f"Erro no consumidor: {msg.error()}")
            continue

        try:
            data = json.loads(msg.value().decode('utf-8'))
            order_id = data['orderId']
            amount = data.get('amount', 0)

            print(f"💰 Processando pagamento: Pedido {order_id}, Valor R${amount}")

            # Salvar evento de recebimento
            save_event(order_id, "PaymentCommandReceived", data)

            # Simular aprovação (80% chance de sucesso)
            if random.random() < 0.8:
                # Pagamento aprovado
                event = {
                    'orderId': order_id,
                    'status': 'approved',
                    'transactionId': f"txn_{order_id[:8]}"
                }
                save_event(order_id, "PaymentApproved", event)
                producer.produce(
                    'payment-approved',
                    key=order_id,
                    value=json.dumps(event).encode('utf-8')
                )
                producer.flush()
                print(f"✅ Pagamento APROVADO: Pedido {order_id}")

            else:
                # Pagamento recusado
                event = {
                    'orderId': order_id,
                    'status': 'rejected',
                    'reason': 'Saldo insuficiente'
                }
                save_event(order_id, "PaymentRejected", event)
                producer.produce(
                    'payment-rejected',
                    key=order_id,
                    value=json.dumps(event).encode('utf-8')
                )
                producer.flush()
                print(f"❌ Pagamento RECUSADO: Pedido {order_id}")

        except Exception as e:
            print(f"Erro ao processar pagamento: {e}")

# Iniciar thread consumidora
thread = threading.Thread(target=consume_payment_commands, daemon=True)
thread.start()

@app.get("/")
def root():
    return {"service": "payments-service", "status": "running"}

@app.get("/payments/{order_id}")
def get_payment_events(order_id: str):
    """Consulta eventos de pagamento de um pedido"""
    db = SessionLocal()
    try:
        events = db.query(PaymentEvent)\
            .filter(PaymentEvent.order_id == order_id)\
            .order_by(PaymentEvent.timestamp)\
            .all()
        
        return {
            "order_id": order_id,
            "events": [
                {
                    "event_type": e.event_type,
                    "data": json.loads(e.data) if e.data else {},
                    "timestamp": e.timestamp.isoformat()
                }
                for e in events
            ]
        }
    finally:
        db.close()
