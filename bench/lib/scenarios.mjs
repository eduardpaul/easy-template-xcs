// Benchmark scenarios. Each scenario uses one of the original easy-template-x
// fixture documents (shared by the .NET test suite) and generates its data
// deterministically, so every engine processes exactly the same input.

import { readFileSync } from 'node:fs';
import path from 'node:path';

const repoDir = path.resolve(import.meta.dirname, '..', '..');
const fixturesDir = path.join(repoDir, 'src', 'Easy.Template.XCS.Test', 'Fixtures');

export const readTemplate = name => readFileSync(path.join(fixturesDir, 'Files', name));
const readResource = name => readFileSync(path.join(fixturesDir, 'Res', name));

// small seeded PRNG (mulberry32) so the data is identical between runs and engines
function random(seed) {
    return () => {
        seed |= 0;
        seed = (seed + 0x6D2B79F5) | 0;
        let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
        t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}

const lorem = ('lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore ' +
    'et dolore magna aliqua ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi aliquip ex ea ' +
    'commodo consequat').split(' ');

function words(rnd, count = 1) {
    return Array.from({ length: count }, () => lorem[Math.floor(rnd() * lorem.length)]).join(' ');
}

function paragraphs(rnd, count = 1) {
    return Array.from({ length: count }, () => words(rnd, 20)).join('\n');
}

function tableRows(count) {
    return () => ({
        outProp: 'I am out!',
        loop: Array.from({ length: count }, (_, i) => ({ prop: `row ${i + 1}` }))
    });
}

export const scenarios = [
    {
        name: 'simple',
        description: 'Single text tag (simple.docx)',
        template: 'simple.docx',
        data: () => ({ simple_prop: 'hello world' })
    },
    {
        name: 'table-rows-100',
        description: 'Table row loop, 100 rows (loop - table.docx)',
        template: 'loop - table.docx',
        data: tableRows(100)
    },
    {
        name: 'table-rows-1000',
        description: 'Table row loop, 1,000 rows (loop - table.docx)',
        template: 'loop - table.docx',
        data: tableRows(1000)
    },
    {
        name: 'nested-loops',
        description: 'Nested paragraph loops, 30 x 30 (loop - nested.docx)',
        template: 'loop - nested.docx',
        data: () => {
            const rnd = random(42);
            return {
                loop_prop1: Array.from({ length: 30 }, () => ({
                    loop_prop2: Array.from({ length: 30 }, () => ({ simple_prop: words(rnd, 2) }))
                }))
            };
        }
    },
    {
        name: 'real-life-he',
        description: 'Real-life Hebrew report card, 20 students x 5 groups, raw xml page breaks (real life - he.docx)',
        template: 'real life - he.docx',
        data: () => {
            const rnd = random(7);
            return {
                'תלמידים': Array.from({ length: 20 }, () => ({
                    'שם התלמיד': words(rnd),
                    'קבוצות': Array.from({ length: 5 }, () => ({
                        'שם הקבוצה': words(rnd),
                        'שם המורה': words(rnd, 2),
                        'הערכה מילולית': paragraphs(rnd, 2)
                    }))
                })),
                'עמוד חדש': { _type: 'rawXml', xml: '<w:br w:type="page"/>' }
            };
        }
    },
    {
        name: 'image',
        description: 'Image placeholder replacement, 32 KB jpeg (image - placeholder.docx)',
        template: 'image - placeholder.docx',
        data: () => ({
            'My Tag 1': 'hello image',
            'My Tag 2': {
                _type: 'image',
                source: readResource('panda1.jpg'),
                format: 'image/jpeg',
                altText: 'There is no spoon.',
                width: 200,
                height: 150
            }
        })
    }
];

export function findScenarios(names) {
    if (!names || names.length === 0)
        return scenarios;
    return names.map(name => {
        const scenario = scenarios.find(s => s.name === name);
        if (!scenario)
            throw new Error(`Unknown scenario '${name}'. Expected one of: ${scenarios.map(s => s.name).join(', ')}`);
        return scenario;
    });
}
