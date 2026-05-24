FROM maven:3.9-eclipse-temurin-21 AS builder
WORKDIR /src
COPY . .
RUN mvn clean package -DskipTests -q

FROM eclipse-temurin:21-jdk-alpine
WORKDIR /app
COPY --from=builder /src/target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
