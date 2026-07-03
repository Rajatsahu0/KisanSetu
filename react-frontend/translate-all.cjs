const fs = require('fs');
const path = require('path');
const { BedrockRuntimeClient, InvokeModelCommand } = require('@aws-sdk/client-bedrock-runtime');

const d = path.join(__dirname, 'src/i18n/locales');
const en = JSON.parse(fs.readFileSync(d + '/en/translation.json', 'utf8'));
const cl = new BedrockRuntimeClient({ region: 'us-east-1' });

const LANGS = [
  { c: 'hi', n: 'Hindi', s: 'Devanagari' },
  { c: 'pa', n: 'Punjabi', s: 'Gurmukhi' },
  { c: 'bn', n: 'Bengali', s: 'Bengali' },
  { c: 'mr', n: 'Marathi', s: 'Devanagari' },
  { c: 'te', n: 'Telugu', s: 'Telugu' },
  { c: 'ta', n: 'Tamil', s: 'Tamil' },
  { c: 'gu', n: 'Gujarati', s: 'Gujarati' },
  { c: 'kn', n: 'Kannada', s: 'Kannada' },
  { c: 'ml', n: 'Malayalam', s: 'Malayalam' },
];

// Flatten nested object (skip arrays)
function flat(o, p = '') {
  const r = {};
  for (const [k, v] of Object.entries(o)) {
    const f = p ? p + '.' + k : k;
    if (Array.isArray(v)) {
      r[f] = '__ARRAY__';
    } else if (typeof v === 'object') {
      Object.assign(r, flat(v, f));
    } else {
      r[f] = v;
    }
  }
  return r;
}

function unflat(o) {
  const r = {};
  for (const [k, v] of Object.entries(o)) {
    if (v === '__ARRAY__') continue;
    const p = k.split('.');
    let c = r;
    for (let i = 0; i < p.length - 1; i++) { if (!c[p[i]]) c[p[i]] = {}; c = c[p[i]]; }
    c[p[p.length - 1]] = v;
  }
  return r;
}

// Get nested value from object by dot path
function getNestedVal(obj, path) {
  return path.split('.').reduce((o, k) => o?.[k], obj);
}

function setNestedVal(obj, path, val) {
  const parts = path.split('.');
  let c = obj;
  for (let i = 0; i < parts.length - 1; i++) { if (!c[parts[i]]) c[parts[i]] = {}; c = c[parts[i]]; }
  c[parts[parts.length - 1]] = val;
}

async function callBedrock(prompt) {
  const body = JSON.stringify({
    messages: [{ role: 'user', content: [{ text: prompt }] }],
    inferenceConfig: { maxTokens: 4000, temperature: 0.1 }
  });
  const resp = await cl.send(new InvokeModelCommand({
    modelId: 'us.amazon.nova-lite-v1:0',
    contentType: 'application/json',
    accept: 'application/json',
    body: Buffer.from(body)
  }));
  return JSON.parse(new TextDecoder().decode(resp.body)).output?.message?.content?.[0]?.text || '';
}

async function translateBatch(entries, name, script) {
  const lines = entries.map(([k, v]) => k + '=' + v).join('\n');
  const prompt = `Translate UI text to ${name} (${script}). Rules: Keep key before = as-is. Keep {{variables}} like {{count}} {{seconds}} {{name}} {{time}} UNCHANGED. Keep PDF,CSV,AI,pH,kg/ha,ppm,MB,KisanMitra AI as-is. Use simple farmer-friendly ${name}.\nINPUT:\n${lines}\nOUTPUT:`;
  const text = await callBedrock(prompt);
  const res = {};
  for (const line of text.split('\n')) {
    const eq = line.indexOf('=');
    if (eq > 0) { const k = line.substring(0, eq).trim(); const v = line.substring(eq + 1).trim(); if (k && v) res[k] = v; }
  }
  return res;
}

async function translateArray(steps, name, script) {
  const numbered = steps.map((s, i) => `${i + 1}. ${s}`).join('\n');
  const prompt = `Translate to ${name} (${script}). Keep numbering. Keep AI, PDF, Soil Health Card as-is. Simple farmer ${name}.\n\n${numbered}\n\nOUTPUT:`;
  const text = await callBedrock(prompt);
  const lines = text.split('\n').filter(l => /^\d+\./.test(l.trim()));
  return lines.map(l => l.replace(/^\d+\.\s*/, '').trim());
}

// Find all array paths in English
function findArrayPaths(obj, prefix = '') {
  const paths = [];
  for (const [k, v] of Object.entries(obj)) {
    const p = prefix ? prefix + '.' + k : k;
    if (Array.isArray(v)) paths.push(p);
    else if (typeof v === 'object') paths.push(...findArrayPaths(v, p));
  }
  return paths;
}

async function main() {
  const flatEn = flat(en);
  const arrayPaths = findArrayPaths(en);
  const totalFlat = Object.keys(flatEn).filter(k => flatEn[k] !== '__ARRAY__').length;
  console.log(`EN: ${totalFlat} flat keys + ${arrayPaths.length} arrays\n`);

  for (const lang of LANGS) {
    const file = d + '/' + lang.c + '/translation.json';
    const existing = JSON.parse(fs.readFileSync(file, 'utf8'));
    const flatEx = flat(existing);

    // 1. Find missing flat keys
    const needs = {};
    for (const [k, v] of Object.entries(flatEn)) {
      if (v === '__ARRAY__') continue;
      if (!flatEx[k] || flatEx[k] === v) needs[k] = v;
    }

    // 2. Find untranslated arrays
    const needArrays = [];
    for (const ap of arrayPaths) {
      const enArr = getNestedVal(en, ap);
      const exArr = getNestedVal(existing, ap);
      if (!exArr || JSON.stringify(exArr) === JSON.stringify(enArr)) needArrays.push(ap);
    }

    if (Object.keys(needs).length === 0 && needArrays.length === 0) {
      console.log(`${lang.c}: Complete`);
      continue;
    }

    console.log(`${lang.c}: ${Object.keys(needs).length} flat keys + ${needArrays.length} arrays`);

    // 3. Translate flat keys in batches
    const allTranslated = {};
    const keys = Object.entries(needs);
    for (let i = 0; i < keys.length; i += 40) {
      const batch = keys.slice(i, i + 40);
      try {
        const tr = await translateBatch(batch, lang.n, lang.s);
        Object.assign(allTranslated, tr);
        process.stdout.write(`  flat batch ${Math.floor(i / 40) + 1}/${Math.ceil(keys.length / 40)}: ${Object.keys(tr).length} `);
      } catch (e) {
        process.stdout.write(`  flat batch FAIL `);
        for (const [k, v] of batch) allTranslated[k] = v;
      }
      if (i + 40 < keys.length) await new Promise(r => setTimeout(r, 400));
    }
    if (keys.length > 0) console.log('');

    // 4. Translate arrays
    for (const ap of needArrays) {
      const enArr = getNestedVal(en, ap);
      try {
        const translated = await translateArray(enArr, lang.n, lang.s);
        if (translated.length === enArr.length) {
          setNestedVal(existing, ap, translated);
          console.log(`  array ${ap}: ${translated.length} items`);
        } else {
          console.log(`  array ${ap}: MISMATCH (${translated.length}/${enArr.length}), retry...`);
          const retry = await translateArray(enArr, lang.n, lang.s);
          if (retry.length === enArr.length) {
            setNestedVal(existing, ap, retry);
            console.log(`  array ${ap}: retry OK`);
          }
        }
      } catch (e) {
        console.log(`  array ${ap}: FAIL`);
      }
      await new Promise(r => setTimeout(r, 300));
    }

    // 5. Merge flat translations
    const merged = flat(existing);
    // Keep existing non-English translations
    for (const [k, v] of Object.entries(flatEn)) {
      if (v === '__ARRAY__') continue;
      if (!merged[k]) merged[k] = v; // Add missing keys with English default
    }
    // Apply new translations
    for (const [k, v] of Object.entries(allTranslated)) {
      merged[k] = v;
    }
    // Keep existing translations that differ from English
    for (const [k, v] of Object.entries(flatEx)) {
      if (v !== '__ARRAY__' && v !== flatEn[k]) merged[k] = v;
    }

    // 6. Rebuild and save
    const result = unflat(merged);
    // Restore arrays from existing (they were set via setNestedVal)
    for (const ap of arrayPaths) {
      const arr = getNestedVal(existing, ap);
      if (arr && Array.isArray(arr)) setNestedVal(result, ap, arr);
    }

    fs.writeFileSync(file, JSON.stringify(result, null, 2) + '\n');

    // Count coverage
    const finalFlat = flat(result);
    let total = 0, translated = 0;
    for (const [k, v] of Object.entries(flatEn)) {
      if (v === '__ARRAY__') continue;
      total++;
      if (finalFlat[k] && finalFlat[k] !== v) translated++;
    }
    console.log(`  Done: ${translated}/${total} flat keys\n`);
  }

  console.log('=== COMPLETE ===');
}

main().catch(console.error);
