from transitions import Machine
import math

class SolarStateMachine:
    def __init__(self):
        # Имена состояний - часы от 0 до 23
        self.states = [f"Hour_{i}" for i in range(24)]
        self.machine = Machine(model=self, states=self.states, initial="Hour_0")

        # Определяем переходы между состояниями
        for i in range(24):
            next_state = f"Hour_{(i + 1) % 24}"  # Переход на следующий час
            self.machine.add_transition(trigger=f"to_hour_{(i + 1) % 24}", source=f"Hour_{i}", dest=next_state)

    def calculate_sun_angle(self, hour):
        """
        Вычисление угла солнца на основе часа.
        Простая модель: угол меняется от -90° до +90° в течение суток.
        """
        # Приводим часы к диапазону от 0 до 23
        normalized_hour = hour % 24

        # Вычисляем угол солнца (примерная модель, максимум в полдень)
        sun_angle = 90 * math.sin(2 * math.pi * (normalized_hour / 24))
        return sun_angle

    def simulate_day(self):
        """
        Симуляция полного дня с выводом состояний и углов солнца.
        """
        for hour in range(24):
            sun_angle = self.calculate_sun_angle(hour)
            print(f"Час: {hour}, Угол солнца: {sun_angle:.2f}°")
            getattr(self, f"to_hour_{(hour + 1) % 24}")()

# Инициализация автомата
solar_machine = SolarStateMachine()

# Симуляция суток
solar_machine.simulate_day()
