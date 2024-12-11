const { spawn } = require('child_process');
const fs = require('fs/promises');
const path = require('path');

// Configuration de base
const BASE_CONFIG = {
    testDuration: '30s',
    initialConnections: 50,
    maxConnections: 1000,
    connectionStep: 50,
    initialRate: 1000,
    maxRate: 10000,
    rateStep: 500,
    successThreshold: 0.95 // 95% de succès requis
};

class LoadTester {
    constructor(config = BASE_CONFIG) {
        this.config = config;
        this.results = [];
        this.bestResult = null;
    }

    async prepare() {
        const tapRequest = {
            cardId: "CARD123",
            stationId: "STATION456",
            tapType: 1,
            timestamp: new Date().toISOString(),
            payload: Buffer.from("test-payload").toString('base64')
        };

        this.tempFile = path.join(__dirname, 'tap-request.json');
        await fs.writeFile(this.tempFile, JSON.stringify(tapRequest));
    }

    async cleanup() {
        if (this.tempFile) {
            await fs.unlink(this.tempFile);
        }
    }

    async runSingleTest(connections, rate) {
        return new Promise((resolve) => {
            const results = {
                connections,
                rate,
                success: false,
                stats: {}
            };

            const bombardier = spawn('bombardier', [
                '-c', connections.toString(),
                '-d', this.config.testDuration,
                '-r', rate.toString(),
                '-f', this.tempFile,
                '-H', 'Content-Type: application/json',
                '-m', 'POST',
                'http://localhost:5000/api/tap'
            ]);

            let output = '';

            bombardier.stdout.on('data', (data) => {
                output += data.toString();
                console.log(data.toString());
            });

            bombardier.stderr.on('data', (data) => {
                console.error(data.toString());
            });

            bombardier.on('close', (code) => {
                // Analyser la sortie pour extraire les statistiques
                const stats = this.parseStats(output);
                results.stats = stats;
                
                // Vérifier si le test est réussi
                results.success = code === 0 && 
                    stats.successRate >= this.config.successThreshold;

                resolve(results);
            });
        });
    }

    parseStats(output) {
        const stats = {
            reqs: 0,
            successRate: 0,
            latencyP99: 0
        };

        // Exemple de parsing basique (à adapter selon la sortie exacte de bombardier)
        const lines = output.split('\n');
        for (const line of lines) {
            if (line.includes('Reqs/sec')) {
                stats.reqs = parseFloat(line.match(/[\d.]+/)[0]);
            }
            if (line.includes('Success ratio')) {
                stats.successRate = parseFloat(line.match(/[\d.]+/)[0]) / 100;
            }
            if (line.includes('99th percentile')) {
                stats.latencyP99 = parseFloat(line.match(/[\d.]+/)[0]);
            }
        }

        return stats;
    }

    async findMaximumLoad() {
        await this.prepare();

        try {
            // Test de charge incrémental pour les connexions
            console.log('=== Test de montée en charge des connexions ===');
            for (let conn = this.config.initialConnections; 
                 conn <= this.config.maxConnections; 
                 conn += this.config.connectionStep) {
                
                const result = await this.runSingleTest(conn, this.config.initialRate);
                this.results.push(result);

                if (!result.success) {
                    console.log(`Point de rupture des connexions atteint à ${conn} connexions`);
                    this.bestResult = this.results[this.results.length - 2];
                    break;
                }
            }

            // Test de charge incrémental pour le débit
            if (this.bestResult) {
                console.log('=== Test de montée en charge du débit ===');
                const optimalConnections = this.bestResult.connections;

                for (let rate = this.config.initialRate; 
                     rate <= this.config.maxRate; 
                     rate += this.config.rateStep) {
                    
                    const result = await this.runSingleTest(optimalConnections, rate);
                    this.results.push(result);

                    if (!result.success) {
                        console.log(`Point de rupture du débit atteint à ${rate} req/sec`);
                        this.bestResult = this.results[this.results.length - 2];
                        break;
                    }
                }
            }

            // Afficher le résumé
            this.displaySummary();
        } finally {
            await this.cleanup();
        }
    }

    displaySummary() {
        console.log('\n=== Résumé des Tests ===');
        if (this.bestResult) {
            console.log(`Configuration optimale trouvée :`);
            console.log(`- Nombre maximum de connexions : ${this.bestResult.connections}`);
            console.log(`- Débit maximum : ${this.bestResult.rate} req/sec`);
            console.log(`- Taux de succès : ${(this.bestResult.stats.successRate * 100).toFixed(2)}%`);
            console.log(`- Latence P99 : ${this.bestResult.stats.latencyP99}ms`);
        } else {
            console.log('Aucune configuration stable trouvée');
        }
    }
}

// Lancement des tests
async function runLoadTest() {
    const tester = new LoadTester();
    await tester.findMaximumLoad();
}

runLoadTest().catch(console.error);