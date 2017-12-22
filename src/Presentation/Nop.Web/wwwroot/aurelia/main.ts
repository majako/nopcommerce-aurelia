import { Aurelia, PLATFORM } from 'aurelia-framework';

export async function configure(aurelia: Aurelia) {
    aurelia.use
        .defaultBindingLanguage()
        .defaultResources()
        .developmentLogging()
        .globalResources(PLATFORM.moduleName('welcome')); // <- Make the component a global resource
    await aurelia.start();
    await aurelia.enhance(document.querySelector('welcome')); // <- Enhence the component
}